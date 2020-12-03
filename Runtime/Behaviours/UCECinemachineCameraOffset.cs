using UnityEngine;
using Cinemachine.Utility;
using Cinemachine;

namespace UCE.Runtime.Behaviours
{
    /// <summary>
    /// 镜头偏移扩展
    /// 在原本 CinemachineCameraOffset 进行了部分修改
    /// 添加了 Active 字段，用于控制其逻辑是否生效
    /// 
    /// An add-on module for Cinemachine Virtual Camera that adds a final offset to the camera
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
[ExecuteInEditMode]
#endif
    [DisallowMultipleComponent]
    public class UCECinemachineCameraOffset : CinemachineExtension
    {
        /// <summary>
        /// Offset the camera's position by this much (camera space)
        /// </summary>
        [Tooltip("Offset the camera's position by this much (camera space)")]
        public Vector3 m_Offset = Vector3.zero;

        /// <summary>
        /// When to apply the offset
        /// </summary>
        [Tooltip("When to apply the offset")] public CinemachineCore.Stage m_ApplyAfter = CinemachineCore.Stage.Aim;

        /// <summary>
        /// If applying offset after aim, re-adjust the aim to preserve the screen position
        /// of the LookAt target as much as possible
        /// </summary>
        [Tooltip("If applying offset after aim, re-adjust the aim to preserve the screen position"
                 + " of the LookAt target as much as possible")]
        public bool m_PreserveComposition;

        /// <summary>
        /// Control this extension will enable
        /// </summary>
        public bool m_Active;

        /// <summary>
        /// Applies the specified offset to the camera state
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (!m_Active)
                return;

            if (stage == m_ApplyAfter)
            {
                bool preserveAim = m_PreserveComposition
                                   && state.HasLookAt && stage > CinemachineCore.Stage.Body;

                Vector3 screenOffset = Vector2.zero;
                if (preserveAim)
                {
                    screenOffset = state.RawOrientation.GetCameraRotationToTarget(
                        state.ReferenceLookAt - state.CorrectedPosition, state.ReferenceUp);
                }

                Vector3 offset = state.RawOrientation * m_Offset;
                state.PositionCorrection += offset;
                if (!preserveAim)
                    state.ReferenceLookAt += offset;
                else
                {
                    var q = Quaternion.LookRotation(
                        state.ReferenceLookAt - state.CorrectedPosition, state.ReferenceUp);
                    q = q.ApplyCameraRotation(-screenOffset, state.ReferenceUp);
                    state.RawOrientation = q;
                }
            }
        }
    }
}