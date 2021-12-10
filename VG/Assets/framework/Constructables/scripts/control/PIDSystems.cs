using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.PID
{
    [System.Serializable]
    public class PID
    {
        public float K = 1;
        public float Ki = 0.1f;
        public float Kd = 0.1f;

        public float resetLimit = 0.1f;

        private float reset;
        private float error;
        private float lastError;

        public float currentValue { get; private set; }

        public float Evaluate(float target, float current)
        {
            error = target - current;

            reset = Mathf.Clamp(reset + Ki * error * Time.deltaTime, -resetLimit, resetLimit);

            float result = (K * error) + reset + (Kd * (error - lastError) );

            lastError = error;

            currentValue = result;

            return result;
        }
    }
    [System.Serializable]
    public class PID2
    {
        public float K = 1;
        public float Ki = 0.1f;
        public float Kd = 0.1f;

        private Vector2 reset;
        private Vector2 error;
        private Vector2 lastError;

        public Vector2 Evaluate(Vector2 target, Vector2 current)
        {
            error = target - current;

            reset += Ki * error * Time.deltaTime;

            Vector2 result = (K * error) + reset + (Kd * (error - lastError));

            lastError = error;

            return result;
        }
    }
    [System.Serializable]
    public class PID3
    {
        public float K = 1;
        public float Ki = 0.1f;
        public float Kd = 0.1f;

        private Vector3 reset;
        private Vector3 error;
        private Vector3 lastError;

        public Vector3 Evaluate(Vector3 target, Vector3 current)
        {
            error = target - current;

            reset += Ki * error * Time.deltaTime;

            Vector3 result = (K * error) + reset + (Kd * (error - lastError));

            lastError = error;

            return result;
        }
    }
}