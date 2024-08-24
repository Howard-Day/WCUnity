using UnityEngine.Rendering;
using UnityEngine;

namespace PI.NGSS
{
    [ExecuteInEditMode]
    public class NGSS_Local : MonoBehaviour
    {
        [Header("GLOBAL SETTINGS FOR LOCAL LIGHTS")]
        //[Tooltip("Check this option to disable this component from receiving updates calls at runtime or when you hit play in Editor.")]
        //public bool NGSS_DISABLE_ON_PLAY = false;

        [Tooltip("Check this option if you don't need to update shadows variables at runtime, only once when scene loads.")]
        public bool NGSS_NO_UPDATE_ON_PLAY = false;

        [Tooltip("Check this option if you want to force hard shadows even when NGSS is installed. Useful to force a fallback to cheap shadows manually.")]
        public bool NGSS_FORCE_HARD_SHADOWS = false;

        [Tooltip("Check this option if you want to be warn about having multiple instances of NGSS_Local in your scene, which was deprecated in v2.1")]
        public bool NGSS_MULTIPLE_INSTANCES_WARNING = true;

        [Space] [Tooltip("Used to test blocker search and early bail out algorithms. Keep it as low as possible, might lead to noise artifacts if too low.\nRecommended values: Mobile = 8, Consoles & VR = 16, Desktop = 24")] [Range(4, 32)]
        public int NGSS_SAMPLING_TEST = 16;

        [Tooltip("Number of samplers per pixel used for PCF and PCSS shadows algorithms.\nRecommended values: Mobile = 12, Consoles & VR = 24, Desktop Med = 32, Desktop High = 48, Desktop Ultra = 64")] [Range(4, 64)]
        public int NGSS_SAMPLING_FILTER = 32;

        [Tooltip("New optimization that reduces sampling over distance. Interpolates current sampling set (TEST and FILTER) down to 4spp when reaching this distance.")] [Range(0f, 500f)]
        public float NGSS_SAMPLING_DISTANCE = 75f;
#if !UNITY_5
        [Tooltip("Normal Offset Bias algorith. Scale position along vertex normals inwards using this value. A value of 0.01 provides good results. Requires the install of NGSS Shadows Bias library.")] [Range(0f, 1f)]
        public float NGSS_NORMAL_BIAS = 0.1f;
#endif

        [Space] [Tooltip("If zero = 100% noise.\nIf one = 100% dithering.\nUseful when fighting banding.")] [Range(0, 1)]
        public int NGSS_NOISE_TO_DITHERING_SCALE = 0;

        //[Tooltip("Interpolates from noise to banding over a given distance, making far away samplers more stable and reducing noise shimmering.")]
        //[Range(0, 500)]
        //public float NGSS_NOISE_DISTANCE = 15f;

        [Tooltip("If you set the noise scale value to something less than 1 you need to input a noise texture.\nRecommended noise textures are blue noise signals.")]
        public Texture2D NGSS_NOISE_TEXTURE;

        [Space] [Tooltip("Number of samplers per pixel used for PCF and PCSS shadows algorithms.\nRecommended values: Mobile = 12, Consoles & VR = 24, Desktop Med = 32, Desktop High = 48, Desktop Ultra = 64")] [Range(0f, 1f)]
        public float NGSS_SHADOWS_OPACITY = 1f;

#if !UNITY_5
        //[Header("PCSS")]
        //[Tooltip("PCSS Requires inline sampling and SM3.5.\nProvides Area Light soft-shadows.\nDisable it if you are looking for PCF filtering (uniform soft-shadows) which runs with SM3.0.")]
        //public bool NGSS_PCSS_ENABLED = true;

        [Tooltip("How soft shadows are when close to caster. Low values means sharper shadows.")] [Range(0f, 2f)]
        public float NGSS_PCSS_SOFTNESS_NEAR = 0f;

        [Tooltip("How soft shadows are when far from caster. Low values means sharper shadows.")] [Range(0f, 2f)]
        public float NGSS_PCSS_SOFTNESS_FAR = 1f;
#endif

        //[Header("BIAS")]
        //[Tooltip("This estimates receiver slope using derivatives and tries to tilt the filtering kernel along it.\nHowever, when doing it in screenspace from the depth texture can leads to shadow artifacts.\nThus it is disabled by default.")]
        //public bool NGSS_SLOPE_BASED_BIAS = false;
        //[Tooltip("Minimal fractional error for the receiver plane bias algorithm.")]
        //[Range(0f, 0.1f)]
        //public float NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR = 0.01f;

        /****************************************************************/

        //public Texture noiseTexture;
        private bool isInitialized = false;

        void OnDisable()
        {
            isInitialized = false;
        }

        void OnEnable()
        {
            if (IsNotSupported())
            {
                Debug.LogWarning("Unsupported graphics API, NGSS requires at least SM3.0 or higher and DX10 or higher.", this);
                enabled = false;
                return;
            }

            Init();
        }

        void Init()
        {
#if UNITY_EDITOR
            if (NGSS_MULTIPLE_INSTANCES_WARNING)
            {
                var ngss_local_instances = FindObjectsOfType<NGSS_Local>();
                if (ngss_local_instances.Length > 1)
                {
                    Debug.LogWarning("Only one instance of NGSS_Local class is allowed in the scene.\nIf you want to tweak softness and filter type for any local light," +
                                     "you can do it by changing the Light properties ShadowsType and Shadows Strength.\n" +
                                     "The NGSS_Local instance: " + ngss_local_instances[0].name + " will be used.", this);

                    for (int i = 1; i < ngss_local_instances.Length; i++)
                        ngss_local_instances[i].enabled = false;
                }
            }
#endif

            if (isInitialized)
            {
                return;
            }

#if !UNITY_5
            Shader.SetGlobalFloat("NGSS_PCSS_FILTER_LOCAL_MIN", NGSS_PCSS_SOFTNESS_NEAR);
            Shader.SetGlobalFloat("NGSS_PCSS_FILTER_LOCAL_MAX", NGSS_PCSS_SOFTNESS_FAR);
#endif

            SetProperties();

            if (NGSS_NOISE_TEXTURE == null)
            {
                NGSS_NOISE_TEXTURE = Resources.Load<Texture2D>("BlueNoise_R8_8");
            }

            Shader.SetGlobalTexture("_BlueNoiseTexture", NGSS_NOISE_TEXTURE);

            isInitialized = true;
        }

        bool IsNotSupported()
        {
#if UNITY_2018_1_OR_NEWER
            return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2);
#elif UNITY_2017_4_OR_EARLIER
        return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationVita || SystemInfo.graphicsDeviceType == GraphicsDeviceType.N3DS);
#else
        return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D9 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationMobile || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationVita || SystemInfo.graphicsDeviceType == GraphicsDeviceType.N3DS);
#endif
        }

        void Update()
        {
            if (Application.isPlaying)
            {
                //if (NGSS_DISABLE_ON_PLAY) { enabled = false; return; }
                if (NGSS_NO_UPDATE_ON_PLAY)
                {
                    return;
                }
            }

            SetProperties();
        }

        void SetProperties()
        {
            //Global
            NGSS_SAMPLING_TEST = Mathf.Clamp(NGSS_SAMPLING_TEST, 4, NGSS_SAMPLING_FILTER);
            Shader.SetGlobalFloat("NGSS_TEST_SAMPLERS", NGSS_SAMPLING_TEST);

            Shader.SetGlobalFloat("NGSS_FORCE_HARD_SHADOWS", NGSS_FORCE_HARD_SHADOWS ? 1 : 0);

#if !UNITY_5
            Shader.SetGlobalFloat("NGSS_PCSS_FILTER_LOCAL_MIN", NGSS_PCSS_SOFTNESS_NEAR);
            Shader.SetGlobalFloat("NGSS_PCSS_FILTER_LOCAL_MAX", NGSS_PCSS_SOFTNESS_FAR);
            //Shader.SetGlobalFloat("NGSS_PCSS_LOCAL_BLOCKER_BIAS", NGSS_PCSS_BLOCKER_BIAS * 0.0005f);
#endif
            Shader.SetGlobalFloat("NGSS_NOISE_TO_DITHERING_SCALE", NGSS_NOISE_TO_DITHERING_SCALE);
            Shader.SetGlobalFloat("NGSS_FILTER_SAMPLERS", NGSS_SAMPLING_FILTER);
            Shader.SetGlobalFloat("NGSS_GLOBAL_OPACITY", 1f - NGSS_SHADOWS_OPACITY);

            Shader.SetGlobalFloat("NGSS_LOCAL_SAMPLING_DISTANCE", NGSS_SAMPLING_DISTANCE);
            //Shader.SetGlobalFloat("NGSS_LOCAL_NOISE_DISTANCE", NGSS_NOISE_DISTANCE);
#if !UNITY_5
            Shader.SetGlobalFloat("NGSS_LOCAL_NORMAL_BIAS", NGSS_NORMAL_BIAS * 0.1f);
#endif
            //Shader.SetGlobalFloat("NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR_LOCAL", NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR);
        }
    }
}