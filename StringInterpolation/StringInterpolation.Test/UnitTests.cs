using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace StringInterpolation.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        [TestMethod]
        public void DiagnoseNoCode()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Fix_Without_Using_System()
        {
            var test = @"
namespace ConsoleApplication1
{
    class TypeName
    {
        void Method()
        {
            var str = $""TestString {2.2}"";
        }
    }
}";
            var fixtest = @"
namespace ConsoleApplication1
{
    class TypeName
    {
        void Method()
        {
            var str = System.FormattableString.Invariant($""TestString {2.2}"");
        }
    }
}";

            VerifyCSharpFix(test, fixtest);
        }


        [TestMethod]
        public void Fix_With_Using_System()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        void Method()
        {
            var str = $""TestString {2.2}"";
        }
    }
}";
            var fixtest = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        void Method()
        {
            var str = FormattableString.Invariant($""TestString {2.2}"");
        }
    }
}";

            VerifyCSharpFix(test, fixtest);
        }


        [TestMethod]
        public void Fix_With_Using_Static_System_FormattableString()
        {
            var test = @"
using static System.FormattableString;

namespace ConsoleApplication1
{
    class TypeName
    {
        void Method()
        {
            var str = $""TestString {2.2}"";
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "StringInterpolation",
                Message = "String Interpolation {2.2} uses IFormatProvider from current Thread",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 37)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
            var fixtest = @"
using static System.FormattableString;

namespace ConsoleApplication1
{
    class TypeName
    {
        void Method()
        {
            var str = Invariant($""TestString {2.2}"");
        }
    }
}";

           VerifyCSharpFix(test, fixtest);
        }



        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new StringInterpolationCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new StringInterpolationAnalyzer();
        }
    }
}