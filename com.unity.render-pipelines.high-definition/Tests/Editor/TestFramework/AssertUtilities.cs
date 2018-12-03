using NUnit.Framework;
using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.TestFramework
{
    public static class AssertUtilities
    {
        public static void AssertAreEqual(Vector3 l, Vector3 r)
        {
            Assert.True(
                Mathf.Approximately(l.x, r.x)
                && Mathf.Approximately(l.y, r.y)
                && Mathf.Approximately(l.z, r.z)
            );
        }

        public static void AssertAreEqual(Quaternion l, Quaternion r)
        {
            AssertAreEqual(l.eulerAngles, r.eulerAngles);
        }

        public static void AssertAreEqual(Matrix4x4 l, Matrix4x4 r)
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 4; ++x)
                    Assert.True(Mathf.Approximately(l[x, y], r[x, y]));
            }
        }
    }
}
