/*

How to use this test framework:

You define test fixtures that contain test cases.
A test fixture is a class that inherits from Tester.TestFixture
A test case is a public method that starts with test_

ex:

public class TestAdd : Tester.TestFixture
{
    public void test_oneplusone()
    {
        Tester.Assert.Equal(1 + 1, 2);
    }
}

To run tests, simply call Tester.TestRunner.Run()

Assertions are available in the Tester.Assert static class

*/

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Tester
{
    /* Registers test fixtures and runs them */
    public static class TestRunner
    {
        /// List of test fixtures to run
        static List<TestFixture> fixtures = new List<TestFixture>();

        static int test_count = 0; /// Number of tests ran
        static int pass_count = 0; /// Number of tests that passed
        static int fail_count = 0; /// Number of tests that failed

        static List<string> log = new List<String>();

        /// Run all tests
        public static void Run()
        {
            GatherTestFixtures();

            foreach(TestFixture fixture in fixtures)
                fixture.RunTests();
            Console.Out.WriteLine(""); // Line break

            DumpLog();
            Console.Out.WriteLine(
                String.Format("{0} tests ran. {1} Passed. {2} Failed.",
                test_count,
                pass_count,
                fail_count)
            );
        }

        /* Retrieves all test fixtures */
        private static void GatherTestFixtures()
        {
            List<Type> derived_classes = Reflection.FindAllDerivedTypes<TestFixture>();
            foreach(Type derived_class in derived_classes)
            {
                TestFixture fixture = (TestFixture)Activator.CreateInstance(derived_class);
                fixtures.Add(fixture);
            }
        }

        /* Dumps the log to stdout */
        private static void DumpLog()
        {
            foreach(string message in log)
                Console.Out.WriteLine(message);
        }

        /// Called when a test succeeds
        public static void TestPassed()
        {
            Console.Out.Write(".");
            test_count++;
            pass_count++;
        }

        /// Called when a test fails
        public static void TestFailed(string test_name, TestFailed error)
        {
            Console.Out.Write("F");
            test_count++;
            fail_count++;

            log.Add(String.Format(
                "{0} FAILED:\n{1}\n", test_name, error.Message
            ));
        }
    }

    /* Test fixture, with a setup and teardown procedure that
    contains tests */
    public class TestFixture
    {
        /// List of test cases in this test fixture
        private List<MethodInfo> test_cases = new List<MethodInfo>();

        /// CTOR
        public TestFixture()
        {
            GatherTestCases();
        }

        /// Gather all functions starting with "test_"
        private void GatherTestCases()
        {
            MethodInfo[] methodInfos = this.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach(MethodInfo methodInfo in methodInfos)
            {
                if(methodInfo.Name.StartsWith("test_"))
                    test_cases.Add(methodInfo);
            }
        }

        /* Run all tests cases */
        public void RunTests()
        {
            foreach(MethodInfo test_case in test_cases)
                RunTestCase(test_case);
        }

        /* Run the given test case */
        private void RunTestCase(MethodInfo test_case)
        {
            try
            {
                test_case.Invoke(this, new object[]{});
                TestRunner.TestPassed();
            }
            catch (TargetInvocationException ex)
            {
                try
                { throw ex.InnerException; }
                catch (TestFailed error)
                { TestRunner.TestFailed(test_case.Name, error); }
            }
        }
    }
}