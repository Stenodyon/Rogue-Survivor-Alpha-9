using System;
using System.Collections.Generic;

namespace Tester
{
    [System.Serializable]
    public class TestFailed : System.Exception
    {
        public TestFailed() {}
        public TestFailed(string message) : base(message) {}
        public TestFailed(string message, System.Exception inner)
            : base(message, inner) {}
        protected TestFailed(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) {}
    }

    /* Class containing assertion functions to use in test cases */
    public static class Assert
    {
        /* Makes the test fail */
        public static void False(string message)
        {
            throw new TestFailed(String.Format(
                "Assertion failed: {0}", message
            ));
        }

        /* Assert lhs and rhs are equal */
        public static void Equal<T>(T lhs, T rhs)
        {
            if(!EqualityComparer<T>.Default.Equals(lhs, rhs))
            {
                throw new TestFailed(
                    String.Format("{0} =/= {1}", lhs, rhs)
                );
            }
        }

        /* Assert lhs and rhs are different */
        public static void NotEqual<T>(T lhs, T rhs)
        {
            if(EqualityComparer<T>.Default.Equals(lhs, rhs))
            {
                throw new TestFailed(
                    String.Format("{0} == {1}", lhs, rhs)
                );
            }
        }
    }
}