using BenchmarkDotNet.Analysers;
using Xunit;

namespace BenchmarkDotNet.Tests.Analysers
{
    public class ZeroMeasurementHelperTests
    {
        // let processor frequency ~ 3.30 GHz
        private const double CPUResolutionMock = 0.2702d / 2;
        
        /* Test distributions inspired by data from method 
           public double Sqrt14() 
                => Math.Sqrt(1) + Math.Sqrt(2) + Math.Sqrt(3) + Math.Sqrt(4) +
                   Math.Sqrt(5) + Math.Sqrt(6) + Math.Sqrt(7) + Math.Sqrt(8) +
                   Math.Sqrt(9) + Math.Sqrt(10) + Math.Sqrt(11) + Math.Sqrt(12) +
                   Math.Sqrt(13) + Math.Sqrt(14);
           with Intel Core i5-2500 CPU 3.30GHz (Sandy Bridge) CPU 
           and RyuJit x64
         */
        [Theory]
        [InlineData(0.27025, 0.27155, 0.27236, 0.27311, 0.27313, 0.27321, 0.27356, 0.27389, 0.27433, 0.27473, 0.27507, 0.27520, 0.27543)]
        [InlineData(0.27875, 0.27876, 0.27961, 0.28004, 0.28211, 0.28270, 0.28323, 0.28361, 0.28404, 0.28452, 0.28456, 0.28584, 0.28651, 0.29015)]
        [InlineData(0.27322, 0.27693, 0.27745, 0.27764, 0.27937, 0.27939, 0.28027, 0.28091, 0.28108, 0.28283, 0.28325, 0.28460, 0.28561, 0.28581)]
        [InlineData(0.27049, 0.27410, 0.27433, 0.27468, 0.27476, 0.27591, 0.27644, 0.27704, 0.27724, 0.27766, 0.27792, 0.27878, 0.28025)]
        public void CheckZeroMeasurement_Around_One_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(distribution, CPUResolutionMock);
            Assert.True(zeroMeasurement);
        }
        
        [Theory]
        [InlineData(0.2702, 0.2702, 0.2702, 0.2702, 0.2702, 0.2702, 0.2702, 0.2702, 0.2702, 0.2702, 0.2702, 0.2702, 0.2702)]
        public void CheckZeroMeasurement_Exactly_One_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(distribution, CPUResolutionMock);
            Assert.True(zeroMeasurement);
        }
        
        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
        public void CheckZeroMeasurement_Exactly_Zero_ns_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(distribution, CPUResolutionMock);
            Assert.True(zeroMeasurement);
        }

        public void CheckZeroMeasurement_Around_Two_CPU_Cycle_Method(params double[] distribution)
        {
            
        }
    }
}