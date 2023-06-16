using System;
using System.IO;
using System.Linq;
using ApprovalTests.Reporters;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class PatchedXUnit2Reporter : XUnit2Reporter
    {
        public override void InvokeEqualsMethod(Type type, string[] parameters)
        {
            var method = type.GetMethods().First(m =>
                m.Name == areEqual &&
                !m.IsGenericMethod &&
                m.GetParameters().Length == 2);
            method.Invoke(null, parameters);
        }

        public override void AssertFileContents(string approved, string received)
        {
            string approvedContent = File.Exists(approved) ? File.ReadAllText(approved).Replace("\r", "") : "";
            string receivedContent = File.ReadAllText(received).Replace("\r", "");
            QuietReporter.DisplayCommandLineApproval(approved, received);
            AssertEqual(approvedContent, receivedContent);
        }
    }
}