using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignLoop.Avatar
{
    /// <summary>
    /// High-performance facial blendshape driver for ISL Non-Manual Markers (NMMs).
    /// Resolves standard ARKit string names to mesh indices once at initialization,
    /// enabling 100% zero-allocation per-frame driving on Meta Quest 3.
    /// </summary>
    public class ARKitFaceController : MonoBehaviour
    {
        [System.Serializable]
        private struct ChannelState
        {
            public string canonicalName;
            public int meshIndex;
            public float currentValue;
            public float targetValue;
        }

        [Header("Target Mesh")]
        [SerializeField] private SkinnedMeshRenderer faceMesh;

        [Header("Animation Settings")]
        [Tooltip("Blend speed in weight units per second (0 to 100).")]
        [SerializeField] private float blendSpeed = 15f;

        private const int ARKitCount = 52;
        private ChannelState[] _channels;
        private int[] _enumToChannelIndex;
        private Dictionary<string, int> _nameToChannelIndex;
        private bool _isInitialized;

        private void Awake()
        {
            if (faceMesh != null)
            {
                Initialize(faceMesh);
            }
        }

        /// <summary>
        /// Binds a new face mesh and rebuilds blendshape name-to-index mapping.
        /// Called by AvatarRigWrapper during avatar swaps.
        /// </summary>
        public void Initialize(SkinnedMeshRenderer newFaceMesh)
        {
            if (newFaceMesh == null)
            {
                Debug.LogWarning("[ARKitFaceController] Attempted to initialize with null SkinnedMeshRenderer.");
                return;
            }

            faceMesh = newFaceMesh;
            Mesh mesh = faceMesh.sharedMesh;
            if (mesh == null)
            {
                Debug.LogError("[ARKitFaceController] SkinnedMeshRenderer has no sharedMesh.");
                return;
            }

            int meshShapeCount = mesh.blendShapeCount;
            var meshNames = new Dictionary<string, int>(meshShapeCount, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < meshShapeCount; i++)
            {
                string rawName = mesh.GetBlendShapeName(i);
                string cleanName = CleanBlendShapeName(rawName);

                if (!meshNames.ContainsKey(cleanName))
                {
                    meshNames[cleanName] = i;
                }
            }

            // Pre-allocate channels for all 52 ARKit shapes
            _channels = new ChannelState[ARKitCount];
            _enumToChannelIndex = new int[ARKitCount];
            _nameToChannelIndex = new Dictionary<string, int>(ARKitCount, StringComparer.OrdinalIgnoreCase);

            var arkitValues = (ARKitBlendShape[])Enum.GetValues(typeof(ARKitBlendShape));

            for (int i = 0; i < arkitValues.Length; i++)
            {
                ARKitBlendShape shape = arkitValues[i];
                string name = shape.ToString();

                int foundIndex = -1;
                if (meshNames.TryGetValue(name, out int idx))
                {
                    foundIndex = idx;
                }

                _channels[i] = new ChannelState
                {
                    canonicalName = name,
                    meshIndex = foundIndex,
                    currentValue = 0f,
                    targetValue = 0f
                };

                _enumToChannelIndex[(int)shape] = i;
                _nameToChannelIndex[name] = i;
            }

            _isInitialized = true;
            Debug.Log($"[ARKitFaceController] Initialized with {meshShapeCount} mesh shapes. Mapped {_channels.Length} ARKit channels.");
        }

        private void Update()
        {
            if (!_isInitialized || faceMesh == null) return;

            float dt = Time.deltaTime;
            float step = blendSpeed * dt * 100f; // Scale to 0-100 weight range

            // Zero garbage collection allocation loop
            for (int i = 0; i < ARKitCount; i++)
            {
                int meshIdx = _channels[i].meshIndex;
                if (meshIdx < 0) continue;

                float current = _channels[i].currentValue;
                float target = _channels[i].targetValue;

                if (Mathf.Abs(current - target) > 0.001f)
                {
                    float next = Mathf.MoveTowards(current, target, step);
                    _channels[i].currentValue = next;
                    faceMesh.SetBlendShapeWeight(meshIdx, next);
                }
            }
        }

        /// <summary>
        /// Sets the target weight for a blendshape by standard ARKit string name (e.g. "browInnerUp", "jawOpen").
        /// Target weight is clamped in [0, 100].
        /// </summary>
        public void SetTargetWeight(string arkitName, float weight)
        {
            if (!_isInitialized) return;

            if (_nameToChannelIndex.TryGetValue(arkitName, out int channelIdx))
            {
                _channels[channelIdx].targetValue = Mathf.Clamp(weight, 0f, 100f);
            }
        }

        /// <summary>
        /// Type-safe, zero-lookup overload using the ARKitBlendShape enum.
        /// </summary>
        public void SetTargetWeight(ARKitBlendShape shape, float weight)
        {
            if (!_isInitialized) return;

            int channelIdx = _enumToChannelIndex[(int)shape];
            _channels[channelIdx].targetValue = Mathf.Clamp(weight, 0f, 100f);
        }

        /// <summary>
        /// Returns whether a specific ARKit shape is supported on the currently bound mesh.
        /// </summary>
        public bool HasBlendShape(ARKitBlendShape shape)
        {
            if (!_isInitialized) return false;
            int channelIdx = _enumToChannelIndex[(int)shape];
            return _channels[channelIdx].meshIndex >= 0;
        }

        /// <summary>
        /// Resets all target weights to zero (smooth return to neutral face).
        /// </summary>
        public void ResetAllTargets()
        {
            if (!_isInitialized) return;

            for (int i = 0; i < ARKitCount; i++)
            {
                _channels[i].targetValue = 0f;
            }
        }

        /// <summary>
        /// Immediately sets all blendshape weights to zero (no smoothing).
        /// </summary>
        public void SnapAllToZero()
        {
            if (!_isInitialized || faceMesh == null) return;

            for (int i = 0; i < ARKitCount; i++)
            {
                _channels[i].targetValue = 0f;
                _channels[i].currentValue = 0f;
                int meshIdx = _channels[i].meshIndex;
                if (meshIdx >= 0)
                {
                    faceMesh.SetBlendShapeWeight(meshIdx, 0f);
                }
            }
        }

        /// <summary>
        /// Strips common FBX prefixes (e.g., "head.", "Wolf3D_Head.", "blendShape.") to isolate the ARKit key.
        /// </summary>
        private static string CleanBlendShapeName(string rawName)
        {
            int lastDot = rawName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < rawName.Length - 1)
            {
                return rawName.Substring(lastDot + 1);
            }

            int lastColon = rawName.LastIndexOf(':');
            if (lastColon >= 0 && lastColon < rawName.Length - 1)
            {
                return rawName.Substring(lastColon + 1);
            }

            return rawName;
        }
    }
}
