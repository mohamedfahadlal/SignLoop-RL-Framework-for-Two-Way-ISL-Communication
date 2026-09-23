namespace SignLoop.Avatar
{
    /// <summary>
    /// Standard 52 ARKit / FACS blendshape identifiers for Non-Manual Markers (NMMs) in ISL.
    /// </summary>
    public enum ARKitBlendShape
    {
        // Eyes Left
        EyeBlinkLeft,
        EyeLookDownLeft,
        EyeLookInLeft,
        EyeLookOutLeft,
        EyeLookUpLeft,
        EyeSquintLeft,
        EyeWideLeft,

        // Eyes Right
        EyeBlinkRight,
        EyeLookDownRight,
        EyeLookInRight,
        EyeLookOutRight,
        EyeLookUpRight,
        EyeSquintRight,
        EyeWideRight,

        // Jaw
        JawForward,
        JawLeft,
        JawRight,
        JawOpen,

        // Mouth
        MouthClose,
        MouthFunnel,
        MouthPucker,
        MouthLeft,
        MouthRight,
        MouthSmileLeft,
        MouthSmileRight,
        MouthFrownLeft,
        MouthFrownRight,
        MouthDimpleLeft,
        MouthDimpleRight,
        MouthStretchLeft,
        MouthStretchRight,
        MouthRollLower,
        MouthRollUpper,
        MouthShrugLower,
        MouthShrugUpper,
        MouthPressLeft,
        MouthPressRight,
        MouthLowerDownLeft,
        MouthLowerDownRight,
        MouthUpperUpLeft,
        MouthUpperUpRight,

        // Brows (Essential for ISL syntax & question types)
        BrowDownLeft,
        BrowDownRight,
        BrowInnerUp,
        BrowOuterUpLeft,
        BrowOuterUpRight,

        // Cheeks
        CheekPuff,
        CheekSquintLeft,
        CheekSquintRight,

        // Nose
        NoseSneerLeft,
        NoseSneerRight,

        // Tongue
        TongueOut
    }
}
