using UnityEngine;
using System;

namespace Utilitay
{

    public enum CurveType
    {
        Constant,               // Fixed value
        Linear,                 // Algebra standard MX+B
        Quadratic,              // Exponential
        Logistic,               // Sigmoid
        Logit,                  // 90 degree Sigmoid (biology/psych origins)
        Threshold,              // Boolean/stair
        Sine,                   // Sine wave
        Parabolic,              // Algebra standard form parabola
        NormalDistribution,     // Probability density function
        Bounce,                 // Bouncing degrading pattern, effectively decaying noise
    }

    [Serializable]
    public class ResponseCurve
    {
        [SerializeField]
        public CurveType CurveShape = CurveType.Linear;

        [SerializeField]
        public float XIntercept = 0.0f;

        [SerializeField]
        public float YIntercept = 0.0f;

        [SerializeField]
        public float SlopeIntercept = 1.0f;

        [SerializeField]
        public float Exponent = 1.0f;
    
        /// Flips the result value of Y to be 1 - Y (top-bottom mirror)
        [SerializeField]
        public bool FlipY;

        /// Flips the value of X to be 1 - X (left-right mirror)
        [SerializeField]
        public bool FlipX;
    
        public float GetValue(float x)
        {
            if (FlipX)
                x = 1.0f - x;

            // Evaluate the curve function for the given inputs.
            float value = 0.0f;
            switch (CurveShape)
            {
            case CurveType.Constant:
                value = YIntercept;
                break;
            case CurveType.Linear:
                // y = m(x - c) + b ... x expanded from standard mx+b
                value = (SlopeIntercept * (x - XIntercept)) + YIntercept;
                break;
            case CurveType.Quadratic:
                // y = mx * (x - c)^K + b
                value = ((SlopeIntercept * x) * Mathf.Pow(Mathf.Abs(x + XIntercept), Exponent)) + YIntercept;
                break;
            case CurveType.Logistic:
                // y = (k * (1 / (1 + (1000m^-1*x + c))) + b
                value = (Exponent * (1.0f / (1.0f + Mathf.Pow(Mathf.Abs(1000.0f * SlopeIntercept), (-1.0f * x) + XIntercept + 0.5f)))) + YIntercept; // Note, addition of 0.5 to keep default 0 XIntercept sane
                break;
            case CurveType.Logit:
                // y = -log(1 / (x + c)^K - 1) * m + b
                value = (-Mathf.Log((1.0f / Mathf.Pow(Mathf.Abs(x - XIntercept), Exponent)) - 1.0f) * 0.05f * SlopeIntercept) + (0.5f + YIntercept); // Note, addition of 0.5f to keep default 0 XIntercept sane
                break;
            case CurveType.Threshold:
                value = x > XIntercept ? (1.0f - YIntercept) : (0.0f - (1.0f - SlopeIntercept));
                break;
            case CurveType.Sine:
                // y = sin(m * (x + c)^K + b
                value = (Mathf.Sin(SlopeIntercept * Mathf.Pow(x + XIntercept, Exponent)) * 0.5f) + 0.5f + YIntercept;
                break;
            case CurveType.Parabolic:
                // y = mx^2 + K * (x + c) + b
                value = Mathf.Pow(SlopeIntercept * (x + XIntercept), 2) + (Exponent * (x + XIntercept)) + YIntercept;
                break;
            case CurveType.NormalDistribution:
                // y = K / sqrt(2 * PI) * 2^-(1/m * (x - c)^2) + b
                value = (Exponent / (Mathf.Sqrt(2 * 3.141596f))) * Mathf.Pow(2.0f, (-(1.0f / (Mathf.Abs(SlopeIntercept) * 0.01f)) * Mathf.Pow(x - (XIntercept + 0.5f), 2.0f))) + YIntercept;
                break;
            case CurveType.Bounce:
                value = Mathf.Abs(Mathf.Sin((6.28f * Exponent) * (x + XIntercept + 1f) * (x + XIntercept + 1f)) * (1f - x) * SlopeIntercept) + YIntercept;
                break;
            }

            // Invert the value if specified as an inverse.
            if (FlipY)
                value = 1.0f - value;

            // Constrain the return to a normal 0-1 range.
            return Mathf.Clamp01(value);
        }

        /// <summary>
        /// Constructs a response curve from a formatted string
        /// Format: CurveType X Y Slope Exponent <FLIPX> <FLIPY>
        /// </summary>
        /// <remarks>
        /// Examples:
        /// Linear 0 0 1 1
        /// Quadratic 0.5 0 0.23 1.3 flipx
        /// Logit -0.15 -0.25 0.3 2.3 flipx flipy
        /// </remarks>
        /// <param name="inputString">String to process</param>
        /// <returns>A response curve created from the input string</returns>
        public static ResponseCurve FromString(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
                throw new ArgumentNullException("inputString");

            string[] words = inputString.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 5)
                throw new FormatException("ResponseCurve.FromString requires 5 SPACE seperated inputs: CurveType X Y Slope Exponent <FLIPX> <FLIPY>");

            ResponseCurve ret = new ResponseCurve();
            ret.CurveShape = (CurveType)Enum.Parse(typeof(CurveType), words[0]);

            float fValue = 0.0f;
            if (float.TryParse(words[1], out fValue))
                ret.XIntercept = fValue;
            else
                throw new FormatException("ResponseCurve.FromString; unable to parse X-Intercept: CurveType X Y Slope Exponent <FLIPX> <FLIPY>");

            if (float.TryParse(words[2], out fValue))
                ret.YIntercept = fValue;
            else
                throw new FormatException("ResponseCurve.FromString; unable to parse Y-Intercept: CurveType X Y Slope Exponent <FLIPX> <FLIPY>");

            if (float.TryParse(words[3], out fValue))
                ret.SlopeIntercept = fValue;
            else
                throw new FormatException("ResponseCurve.FromString; unable to parse SLOPE: CurveType X Y Slope Exponent <FLIPX> <FLIPY>");

            if (float.TryParse(words[4], out fValue))
                ret.Exponent = fValue;
            else
                throw new FormatException("ResponseCurve.FromString; unable to parse EXPONENT: CurveType X Y Slope Exponent <FLIPX> <FLIPY>");

            // If there are more parameters then check to see if they're FlipX/FlipY and set accordingly
            for (int i = 5; i < words.Length; ++i)
            {
                string lCase = words[i].ToLowerInvariant();
                if (lCase.Equals("flipx"))
                    ret.FlipX = true;
                else if (lCase.Equals("flipy"))
                    ret.FlipY = true;
            }

            return ret;
        }
        
        // For the above string parsing split
        private static char[] splitChar = { ' ' };
    }

}