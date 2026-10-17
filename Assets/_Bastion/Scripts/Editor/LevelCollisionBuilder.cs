using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Bastion.Editor
{
    /// <summary>
    /// 그림용 Level/LevelFront 타일맵이 바뀌면 같은 씬의 Collision 타일맵을 자동으로 다시 만든다.
    /// 두 타일맵 중 하나라도 칠해진 칸을 단단한 칸으로 본다 (LevelFront = 벽 옆면처럼 앞에 겹쳐 그리는 칸).
    /// 바닥 타일은 윗면이 보이는 비스듬한 시점이라, 각 기둥의 맨 윗칸을 빼고 충돌을 깔아야
    /// 캐릭터가 윗면 그림 안쪽을 밟는다.
    /// </summary>
    [InitializeOnLoad]
    public static class LevelCollisionBuilder
    {
        static readonly string[] SourceNames = { "Level", "LevelFront" };
        const string CollisionName = "Collision";
        const string CollisionTilePath = "Assets/_Bastion/Art/Tiles/Collision.asset";

        static Tilemap pendingSource;

        static LevelCollisionBuilder()
        {
            Tilemap.tilemapTileChanged += OnTilemapTileChanged;
        }

        static void OnTilemapTileChanged(Tilemap tilemap, Tilemap.SyncTile[] changes)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || tilemap == null
                || System.Array.IndexOf(SourceNames, tilemap.name) < 0)
                return;

            // 칠하는 동안 여러 번 호출되므로 한 프레임에 한 번만 다시 만든다.
            if (pendingSource == null)
                EditorApplication.delayCall += RebuildPending;
            pendingSource = tilemap;
        }

        static void RebuildPending()
        {
            var source = pendingSource;
            pendingSource = null;
            if (source == null)
                return;

            var collision = FindSibling(source, CollisionName);
            if (collision == null)
                return;

            Rebuild(FindSources(source), collision, AssetDatabase.LoadAssetAtPath<TileBase>(CollisionTilePath));
            EditorSceneManager.MarkSceneDirty(collision.gameObject.scene);
        }

        /// <returns>생성한 충돌 칸 수</returns>
        public static int Rebuild(Tilemap[] sources, Tilemap collision, TileBase collisionTile)
        {
            Undo.RegisterCompleteObjectUndo(collision, "Rebuild Level Collision");
            collision.ClearAllTiles();

            bool Solid(Vector3Int cell)
            {
                foreach (var source in sources)
                    if (source.HasTile(cell))
                        return true;
                return false;
            }

            int count = 0;
            foreach (var source in sources)
                foreach (var cell in source.cellBounds.allPositionsWithin)
                {
                    // 위 칸도 채워져 있을 때만 충돌 (= 각 기둥의 맨 윗칸 제외)
                    if (Solid(cell) && Solid(cell + Vector3Int.up) && !collision.HasTile(cell))
                    {
                        collision.SetTile(cell, collisionTile);
                        count++;
                    }
                }
            collision.CompressBounds();
            return count;
        }

        static Tilemap[] FindSources(Tilemap changed)
        {
            var sources = new List<Tilemap>();
            foreach (var name in SourceNames)
            {
                var tilemap = FindSibling(changed, name);
                if (tilemap != null)
                    sources.Add(tilemap);
            }
            return sources.ToArray();
        }

        static Tilemap FindSibling(Tilemap tilemap, string name)
        {
            var parent = tilemap.transform.parent;
            if (parent == null)
                return null;
            var sibling = parent.Find(name);
            return sibling != null ? sibling.GetComponent<Tilemap>() : null;
        }
    }
}
