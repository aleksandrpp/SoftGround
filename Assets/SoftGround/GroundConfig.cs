using UnityEngine;

namespace AK.SoftGround
{
    [CreateAssetMenu(fileName = "SO_GroundConfig", menuName = "AK.SoftGround/GroundConfig")]
    public sealed class GroundConfig : ScriptableObject
    {
        public float
            InputPower = 2.5f;

        public int
            InputCount = 10;

        public ComputeShader Compute;
    }
}