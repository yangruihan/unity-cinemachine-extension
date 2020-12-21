using UnityEngine;

namespace UCE.Runtime.Core
{
    /// <summary>
    /// 跟随目标计算器
    /// 支持 DeadZone 配置，支持 Offset 配置
    /// </summary>
    public class FollowTargetCalculator
    {
        /// <summary>
        /// 摄像机 Transform 缓存
        /// 如果 Rotation 没有变化，不需要每帧重新计算值
        /// </summary>
        private class CameraCache
        {
            public Camera Camera;
            public Vector3 Up;
            public Vector3 Right;
            public Vector3 Forward;
            public Matrix4x4 LocalToWorldMatrix;

            private Quaternion rotation;

            public CameraCache(Camera camera)
            {
                Camera = camera;
                UpdateCache(true);
            }

            public void UpdateCache(bool isForce = false)
            {
                if (!isForce && Camera.transform.rotation == rotation) return;

                var tf = Camera.transform;
                Up = tf.up;
                Right = tf.right;
                Forward = tf.forward;
                LocalToWorldMatrix = tf.localToWorldMatrix;

                rotation = tf.rotation;
            }
        }

        public Rect SoftRect { get; set; }
        public Rect HardRect { get; set; }
        public Vector3 TrackedObjectOffset { get; set; }
        public float FollowSpeed { get; set; }
        public Camera Camera { get; set; }

        private CameraCache _cameraCache;

        public FollowTargetCalculator(Camera camera, Rect softRect, Rect hardRect, Vector3 trackedObjectOffset,
            float followSpeed)
        {
            Camera = camera;
            SoftRect = softRect;
            HardRect = hardRect;
            TrackedObjectOffset = trackedObjectOffset;
            FollowSpeed = followSpeed;

            _cameraCache = new CameraCache(camera);
        }

        public Vector3 UpdatePos(Vector3 currentPos, Vector3 targetPos)
        {
            var targetViewportPos = Camera.WorldToViewportPoint(targetPos + TrackedObjectOffset);
            targetViewportPos.y = 1 - targetViewportPos.y;

            _cameraCache.UpdateCache();

            // 如果在强制拉扯区，则忽略跟踪速度，强制保持角色在范围内
            currentPos = CalculatePos(targetViewportPos, currentPos, targetPos, HardRect, 1);

            // 如果在跟踪区内，则以跟踪速度进行跟踪
            return CalculatePos(targetViewportPos, currentPos, targetPos, SoftRect, FollowSpeed);
        }

        private Vector3 CalculatePos(Vector3 targetViewportPos, Vector3 currentPos, Vector3 targetPos, Rect rect,
            float followSpeed)
        {
            if (!rect.Contains(targetViewportPos))
            {
                var selfViewportPos = Camera.WorldToViewportPoint(currentPos + TrackedObjectOffset);
                selfViewportPos.y = 1 - selfViewportPos.y;

                float xLerp;
                float yLerp;

                if (HardRect.width == 0)
                    xLerp = 1;
                else
                {
                    var xOffset = targetViewportPos.x > selfViewportPos.x
                        ? rect.xMax - selfViewportPos.x
                        : selfViewportPos.x - rect.xMin;

                    xLerp = Mathf.Abs(targetViewportPos.x - selfViewportPos.x) < Mathf.Epsilon
                        ? 0
                        : 1 - xOffset / Mathf.Abs(targetViewportPos.x - selfViewportPos.x);
                }

                if (rect.height == 0)
                    yLerp = 1;
                else
                {
                    var yOffset = targetViewportPos.y > selfViewportPos.y
                        ? rect.yMax - selfViewportPos.y
                        : selfViewportPos.y - rect.yMin;

                    yLerp = Mathf.Abs(targetViewportPos.y - selfViewportPos.y) < Mathf.Epsilon
                        ? 0
                        : 1 - yOffset / Mathf.Abs(targetViewportPos.y - selfViewportPos.y);
                }

                var right = _cameraCache.Right;
                var position = currentPos;
                var x = Mathf.Lerp(
                    Vector3.Dot(position, right),
                    Vector3.Dot(targetPos, right),
                    xLerp);

                var up = _cameraCache.Up;
                var y = Mathf.Lerp(
                    Vector3.Dot(position, up),
                    Vector3.Dot(targetPos, up),
                    yLerp);

                var forward = _cameraCache.Forward;
                var z = Mathf.Lerp(
                    Vector3.Dot(position, forward),
                    Vector3.Dot(targetPos, forward),
                    yLerp);

                return Vector3.Lerp(
                    position,
                    _cameraCache.LocalToWorldMatrix * new Vector3(x, y, z),
                    followSpeed);
            }

            return currentPos;
        }
    }
}