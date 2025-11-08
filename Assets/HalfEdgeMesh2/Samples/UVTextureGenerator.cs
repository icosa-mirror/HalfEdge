using UnityEngine;

namespace HalfEdgeMesh2.Samples
{
    // Generates a procedural UV test texture with checkerboard pattern
    // Useful for visualizing UV mapping on generated meshes
    public static class UVTextureGenerator
    {
        // Creates a checkerboard texture for UV testing
        // gridSize: Number of checker squares per side
        // textureSize: Resolution of the texture
        public static Texture2D CreateCheckerboard(int gridSize = 8, int textureSize = 512)
        {
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;

            var squareSize = textureSize / gridSize;

            for (var y = 0; y < textureSize; y++)
            {
                for (var x = 0; x < textureSize; x++)
                {
                    var squareX = x / squareSize;
                    var squareY = y / squareSize;
                    var isWhite = (squareX + squareY) % 2 == 0;
                    texture.SetPixel(x, y, isWhite ? Color.white : Color.black);
                }
            }

            texture.Apply();
            return texture;
        }

        // Creates a UV gradient texture for UV testing
        // Red channel = U coordinate, Green channel = V coordinate
        public static Texture2D CreateUVGradient(int textureSize = 512)
        {
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (var y = 0; y < textureSize; y++)
            {
                for (var x = 0; x < textureSize; x++)
                {
                    var u = x / (float)textureSize;
                    var v = y / (float)textureSize;
                    texture.SetPixel(x, y, new Color(u, v, 0));
                }
            }

            texture.Apply();
            return texture;
        }

        // Creates a colored grid texture with numbers (conceptual, simplified version)
        public static Texture2D CreateColoredGrid(int gridSize = 4, int textureSize = 512)
        {
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Repeat;

            var squareSize = textureSize / gridSize;
            var colors = new Color[]
            {
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow,
                Color.cyan,
                Color.magenta,
                new Color(1f, 0.5f, 0f), // Orange
                new Color(0.5f, 0f, 1f)  // Purple
            };

            for (var y = 0; y < textureSize; y++)
            {
                for (var x = 0; x < textureSize; x++)
                {
                    var squareX = x / squareSize;
                    var squareY = y / squareSize;
                    var colorIndex = (squareX + squareY * gridSize) % colors.Length;
                    texture.SetPixel(x, y, colors[colorIndex]);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
