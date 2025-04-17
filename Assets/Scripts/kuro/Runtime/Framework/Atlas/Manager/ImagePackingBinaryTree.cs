using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;

namespace kuro
{
    public class ImagePackingBinaryTree : ImagePackingAlgorithm
    {
        private class Node
        {
            private static readonly ObjectPool<Node> s_pool = new(
                static () => new(),
                null,
                x =>
                {
                    x.IsFree = false;
                    x.Rect = default;
                    x.Parent = null;
                    x.Left = null;
                    x.Right = null;
                },
                null,
                false,
                100,
                200
            );

            public static Node Rent()
            {
                var node = s_pool.Get();
                node.IsFree = true;
                return node;
            }

            public static Node Rent(Node parentNode, RectInt rect)
            {
                var node = s_pool.Get();
                node.IsFree = true;
                node.Rect = rect;
                node.Parent = parentNode;
                return node;
            }

            private static void Return(Node node)
            {
                if (node.Left != null)
                    Return(node.Left);
                if (node.Right != null)
                    Return(node.Right);
                s_pool.Release(node);
            }

            public bool IsFree;
            public RectInt Rect;
            public Node Parent;
            public Node Left;
            public Node Right;

            public bool IsLeaf
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Left == null && Right == null;
            }

            public bool IsAllFree
            {
                get
                {
                    if (!IsFree)
                        return false;
                    if (Left is { IsAllFree: false })
                        return false;
                    if (Right is { IsAllFree: false })
                        return false;
                    return true;
                }
            }

            private Node()
            {
            }

            public void Free()
            {
                IsFree = true;

                if (Left != null)
                {
                    Return(Left);
                    Left = null;
                }

                if (Right != null)
                {
                    Return(Right);
                    Right = null;
                }
            }
        };

        private readonly Node _rootNode;
        private readonly Dictionary<int, Node> _nodeMap = new(32);

        protected ImagePackingBinaryTree()
        {
            _rootNode = Node.Rent();
        }

        public ImagePackingBinaryTree(Vector2Int size, int padding) : base(size, padding)
        {
            _rootNode = Node.Rent();
            _rootNode.Rect = new(0, 0, size.x, size.y);
        }

        private Node InsertNode(Node node, Vector2Int size)
        {
            if (node.IsLeaf)
            {
                if (!node.IsFree)
                    return null;

                if (node.Rect.width < size.x || node.Rect.height < size.y)
                    return null;

                if (node.Rect.width == size.x && node.Rect.height == size.y)
                {
                    node.IsFree = false;
                    return node;
                }

                int dw = node.Rect.width - size.x;
                int dh = node.Rect.height - size.y;

                if (dw > dh)
                {
                    node.Left = Node.Rent(node, new(node.Rect.x, node.Rect.y, size.x, node.Rect.height));
                    node.Right = Node.Rent(node, new(node.Rect.x + size.x, node.Rect.y, dw, node.Rect.height));
                }
                else
                {
                    node.Left = Node.Rent(node, new(node.Rect.x, node.Rect.y, node.Rect.width, size.y));
                    node.Right = Node.Rent(node, new(node.Rect.x, node.Rect.y + size.y, node.Rect.width, dh));
                }

                return InsertNode(node.Left, size);
            }
            else
            {
                Node newNode = InsertNode(node.Left, size);
                if (newNode != null)
                    return newNode;

                return InsertNode(node.Right, size);
            }
        }

        protected override bool OnAddImage(int imageId, int width, int height, out Vector2Int pos)
        {
            Node node = InsertNode(_rootNode, new Vector2Int(width, height));
            if (node == null)
            {
                pos = default;
                return false;
            }

            _nodeMap.Add(imageId, node);
            pos = node.Rect.position;

            return true;
        }

        protected override bool OnFreeImage(int imageId)
        {
            if (!_nodeMap.Remove(imageId, out var node))
                return false;

            if (!node.IsLeaf)
                return false;

            while (true)
            {
                node.Free();

                Node parentNode = node.Parent;
                if (parentNode == null)
                    break;

                if (parentNode.Left == node)
                {
                    if (!parentNode.Right.IsAllFree)
                        break;
                }
                else
                {
                    if (!parentNode.Left.IsAllFree)
                        break;
                }

                node = parentNode;
            }

            return true;
        }

        public override void ClearAllImages()
        {
            _nodeMap.Clear();
            _rootNode.Free();

            base.ClearAllImages();
        }
    }
}