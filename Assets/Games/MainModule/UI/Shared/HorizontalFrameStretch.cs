using UnityEngine;
using UnityEngine.UI;

namespace Games.MainModule.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class HorizontalFrameStretch : BaseMeshEffect
    {
        [SerializeField, Range(0f, 0.2f)] private float endFraction = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float centerFraction = 0.2f;

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || vertices.currentVertCount != 4 || vertices.currentIndexCount != 6)
                return;

            var image = (Image)graphic;
            var sprite = image.overrideSprite;
            if (!sprite)
                return;

            var bottomLeft = UIVertex.simpleVert;
            var topLeft = UIVertex.simpleVert;
            var topRight = UIVertex.simpleVert;
            var bottomRight = UIVertex.simpleVert;
            vertices.PopulateUIVertex(ref bottomLeft, 0);
            vertices.PopulateUIVertex(ref topLeft, 1);
            vertices.PopulateUIVertex(ref topRight, 2);
            vertices.PopulateUIVertex(ref bottomRight, 3);

            float width = bottomRight.position.x - bottomLeft.position.x;
            float height = topLeft.position.y - bottomLeft.position.y;
            float originalWidth = height * sprite.rect.width / sprite.rect.height;
            if (originalWidth <= 0f || width <= originalWidth + 0.01f)
                return;

            float endWidth = originalWidth * endFraction;
            float centerWidth = originalWidth * centerFraction;
            float sideWidth = (originalWidth - 2f * endWidth - centerWidth) * 0.5f;
            float extraSideWidth = (width - originalWidth) * 0.5f;
            float sourceStart = 0f;
            float targetStart = bottomLeft.position.x;

            // Only the lengths between the ornaments grow with the canvas.
            vertices.Clear();
            AddSection(endWidth, endWidth);
            AddSection(sideWidth, sideWidth + extraSideWidth);
            AddSection(centerWidth, centerWidth);
            AddSection(sideWidth, sideWidth + extraSideWidth);
            AddSection(endWidth, endWidth);

            void AddSection(float sourceWidth, float targetWidth)
            {
                float sourceEnd = sourceStart + sourceWidth;
                float targetEnd = targetStart + targetWidth;
                int first = vertices.currentVertCount;
                AddVertex(bottomLeft, bottomRight, sourceStart, targetStart);
                AddVertex(topLeft, topRight, sourceStart, targetStart);
                AddVertex(topLeft, topRight, sourceEnd, targetEnd);
                AddVertex(bottomLeft, bottomRight, sourceEnd, targetEnd);
                vertices.AddTriangle(first, first + 1, first + 2);
                vertices.AddTriangle(first + 2, first + 3, first);
                sourceStart = sourceEnd;
                targetStart = targetEnd;
            }

            void AddVertex(UIVertex left, UIVertex right, float sourceX, float targetX)
            {
                left.position.x = targetX;
                left.uv0 = Vector4.LerpUnclamped(left.uv0, right.uv0, sourceX / originalWidth);
                vertices.AddVert(left);
            }
        }
    }
}
