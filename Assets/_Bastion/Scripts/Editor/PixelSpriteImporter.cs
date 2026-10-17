using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Bastion.Editor
{
    /// <summary>
    /// Assets/ThirdParty 아래 PNG에 픽셀 아트 임포트 설정을 자동 적용한다.
    /// 최초 1회만 적용되며(userData 마커), 이후 인스펙터에서 바꾼 값은 유지된다.
    /// </summary>
    public class PixelSpriteImporter : AssetPostprocessor
    {
        const string Root = "Assets/ThirdParty/";
        const string Marker = "bastion-pixel-import-v1";
        const int PixelsPerUnit = 16;

        enum SliceMode { Single, Grid, SquareStrip, HorizontalStrip, AutoAlpha }

        struct Rule
        {
            public SliceMode Mode;
            public Vector2Int Cell;
            public Vector2 Pivot;
        }

        static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        static Rule GetRule(string path)
        {
            string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            if (path.Contains("/FreeKnight_v1/") && path.Contains("PNGSheets"))
                return new Rule { Mode = SliceMode.Grid, Cell = new Vector2Int(120, 80), Pivot = BottomCenter };

            if (path.Contains("/Undead executioner puppet/png/"))
            {
                var cell = file.StartsWith("summon") && file != "summon" ? new Vector2Int(50, 50) : new Vector2Int(100, 100);
                return new Rule { Mode = SliceMode.Grid, Cell = cell, Pivot = BottomCenter };
            }

            if ((path.Contains("/Monsters_Creatures_Fantasy/") || path.Contains("/Monsters Creatures Fantasy 2/"))
                && !file.Contains("preview") && !file.Contains("all characters"))
                return new Rule { Mode = SliceMode.SquareStrip, Pivot = BottomCenter };

            // 함정: 가로 32px 프레임 스트립 (높이는 이미지 높이)
            if (path.Contains("/Traps/"))
                return new Rule { Mode = SliceMode.HorizontalStrip, Cell = new Vector2Int(32, 0), Pivot = BottomCenter };

            // 폭이 제각각인 배경 패널은 알파 기준으로 자동 분할
            if (path.EndsWith("ENVIRONMENT/backgrounds.png"))
                return new Rule { Mode = SliceMode.AutoAlpha, Pivot = BottomCenter };

            if (path.EndsWith("ENVIRONMENT/tileset.png"))
                return new Rule { Mode = SliceMode.Grid, Cell = new Vector2Int(16, 16), Pivot = Center };

            // Gothicvania 캐릭터/FX 개별 프레임(f-01.png 등)
            if (path.Contains("/SPRITES/"))
                return new Rule { Mode = SliceMode.Single, Pivot = BottomCenter };

            return new Rule { Mode = SliceMode.Single, Pivot = Center };
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".png"))
                return;

            var importer = (TextureImporter)assetImporter;
            if (importer.userData == Marker)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = GetRule(assetPath).Pivot;
            importer.SetTextureSettings(settings);

            var rule = GetRule(assetPath);
            if (rule.Mode == SliceMode.Single)
                importer.spriteImportMode = SpriteImportMode.Single;
            else
                ApplySlices(importer, rule);

            importer.userData = Marker;
        }

        void ApplySlices(TextureImporter importer, Rule rule)
        {
            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(File.ReadAllBytes(assetPath)))
            {
                Object.DestroyImmediate(tex);
                return;
            }

            if (rule.Mode == SliceMode.AutoAlpha)
            {
                var autoRects = UnityEditorInternal.InternalSpriteUtility.GenerateAutomaticSpriteRectangles(tex, 16, 0);
                Object.DestroyImmediate(tex);
                WriteSpriteRects(importer, autoRects, rule.Pivot);
                return;
            }

            Vector2Int cell = rule.Mode == SliceMode.SquareStrip ? new Vector2Int(tex.height, tex.height)
                : rule.Mode == SliceMode.HorizontalStrip ? new Vector2Int(rule.Cell.x, tex.height)
                : rule.Cell;

            int cols = tex.width / cell.x;
            int rows = tex.height / cell.y;
            var pixels = tex.GetPixels32();
            var rects = new List<Rect>();
            // 좌상단부터 행 우선 순서 (애니메이션 프레임 순서와 일치)
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var rect = new Rect(c * cell.x, tex.height - (r + 1) * cell.y, cell.x, cell.y);
                    if (IsEmpty(pixels, tex.width, rect))
                        continue;

                    rects.Add(rect);
                }
            }
            Object.DestroyImmediate(tex);
            WriteSpriteRects(importer, rects, rule.Pivot);
        }

        void WriteSpriteRects(TextureImporter importer, IList<Rect> rects, Vector2 pivot)
        {
            string baseName = Path.GetFileNameWithoutExtension(assetPath).Trim('_').Replace(' ', '_');

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // 재임포트 시 기존 스프라이트 ID를 유지해야 애니메이션 클립 등의 참조가 깨지지 않는다.
            var existingIds = new Dictionary<string, GUID>();
            foreach (var existing in provider.GetSpriteRects())
                existingIds[existing.name] = existing.spriteID;

            var spriteRects = new SpriteRect[rects.Count];
            for (int i = 0; i < rects.Count; i++)
            {
                string name = $"{baseName}_{i}";
                spriteRects[i] = new SpriteRect
                {
                    name = name,
                    rect = rects[i],
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = existingIds.TryGetValue(name, out var id) ? id : GUID.Generate()
                };
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            provider.SetSpriteRects(spriteRects);

            var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (var sr in spriteRects)
                pairs.Add(new SpriteNameFileIdPair(sr.name, sr.spriteID));
            nameIds.SetNameFileIdPairs(pairs);

            provider.Apply();
        }

        static bool IsEmpty(Color32[] pixels, int width, Rect rect)
        {
            int xMin = (int)rect.x, yMin = (int)rect.y;
            int xMax = xMin + (int)rect.width, yMax = yMin + (int)rect.height;
            for (int y = yMin; y < yMax; y++)
                for (int x = xMin; x < xMax; x++)
                    if (pixels[y * width + x].a > 0)
                        return false;
            return true;
        }
    }
}
