using BenchmarkDotNet.Mathematics;
using Xunit;

namespace BenchmarkDotNet.Tests.Mathematics
{
    public class WelchTests
    {
        [Fact]
        public void Welch30Vs40Test()
        {
            // R-script for validation:
            // set.seed(42); x <- rnorm(30, mean = 10)
            // set.seed(42); y <- rnorm(40, mean = 10.1)
            // t.test(x, y)
            //
            // #     Welch Two Sample t-test
            // # 
            // # data:  x and y
            // # t = 0.027097, df = 61.716, p-value = 0.9785
            // # alternative hypothesis: true difference in means is not equal to 0
            // # 95 percent confidence interval:
            // #  -0.5911536  0.6073991
            // # sample estimates:
            // # mean of x mean of y 
            // #  10.06859  10.06046

            double[] x =
            {
                11.3709584471467, 9.43530182860391, 10.3631284113373, 10.632862604961,
                10.404268323141, 9.89387548390852, 11.5115219974389, 9.9053409615869,
                12.018423713877, 9.93728590094758, 11.3048696542235, 12.2866453927011,
                8.61113929888766, 9.72121123318263, 9.86667866360634, 10.6359503980701,
                9.71574707858393, 7.34354457909522, 7.55953307142448, 11.3201133457302,
                9.69336140592153, 8.21869156602, 9.82808264424038, 11.2146746991726,
                11.895193461265, 9.5695308683938, 9.74273061723107, 8.23683691480522,
                10.4600973548313, 9.36000512403988
            };
            double[] y =
            {
                11.4709584471467, 9.53530182860391, 10.4631284113373, 10.732862604961,
                10.504268323141, 9.99387548390852, 11.6115219974389, 10.0053409615869,
                12.118423713877, 10.0372859009476, 11.4048696542235, 12.3866453927011,
                8.71113929888766, 9.82121123318263, 9.96667866360634, 10.7359503980701,
                9.81574707858393, 7.44354457909522, 7.65953307142448, 11.4201133457302,
                9.79336140592152, 8.31869156602, 9.92808264424038, 11.3146746991726,
                11.995193461265, 9.6695308683938, 9.84273061723107, 8.33683691480522,
                10.5600973548313, 9.46000512403988, 10.5554501232412, 10.8048373372288,
                11.1351035219699, 9.49107362459279, 10.604955123298, 8.38299132092666,
                9.3155409916205, 9.24909240582348, 7.68579235005337, 10.1361226068923
            };
            var welch = WelchTTest.Calc(new Statistics(x), new Statistics(y));
            Assert.Equal(0.027097, welch.T, 6);
            Assert.Equal(61.716, welch.Df, 3);
            Assert.Equal(0.9785, welch.PValue, 4);
        }

        [Fact]
        public void Welch3Vs3Test()
        {
            // R-script for validation:
            // t.test(c(10, 10.5, 11), c(10, 10, 11))
            //
            // #     Welch Two Sample t-test
            // # 
            // # data:  c(10, 10.5, 11) and c(10, 10, 11)
            // # t = 0.37796, df = 3.92, p-value = 0.725
            // # alternative hypothesis: true difference in means is not equal to 0
            // # 95 percent confidence interval:
            // #  -1.067548  1.400881
            // # sample estimates:
            // # mean of x mean of y 
            // #  10.50000  10.33333

            double[] x = { 10, 10.5, 11 };
            double[] y = { 10, 10, 11 };
            var welch = WelchTTest.Calc(new Statistics(x), new Statistics(y));
            Assert.Equal(0.37796, welch.T, 5);
            Assert.Equal(3.92, welch.Df, 2);
            Assert.Equal(0.725, welch.PValue, 3);
        }

        [Fact]
        public void Welch2Vs4Test()
        {
            // R-script for validation:
            // t.test(c(10, 10.5), c(10, 10, 11, 11))
            //
            // #    Welch Two Sample t-test
            // # 
            // # data:  c(10, 10.5) and c(10, 10, 11, 11)
            // # t = -0.65465, df = 3.4186, p-value = 0.5541
            // # alternative hypothesis: true difference in means is not equal to 0
            // # 95 percent confidence interval:
            // #  -1.3853169  0.8853169
            // # sample estimates:
            // # mean of x mean of y 
            // #     10.25     10.50

            double[] x = { 10, 10.5 };
            double[] y = { 10, 10, 11, 11 };
            var welch = WelchTTest.Calc(new Statistics(x), new Statistics(y));
            Assert.Equal(-0.65465, welch.T, 5);
            Assert.Equal(3.4186, welch.Df, 4);
            Assert.Equal(0.5541, welch.PValue, 4);
        }
    }
}