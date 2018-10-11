using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Mathematics;
using Xunit;

namespace BenchmarkDotNet.Tests.Analysers
{
    public class ZeroMeasurementHelperTests
    {
        // let processor frequency ~ 3.30 GHz
        private static double ThresholdMock = 0.2702d / 2;
        
        /*
          Test distributions inspired by data from method
          
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
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(distribution, ThresholdMock);
            Assert.False(zeroMeasurement);
        }
        
        [Theory]
        [InlineData(0.2703, 0.2701, 0.2703, 0.2701, 0.2703, 0.2701, 0.2703, 0.2701, 0.2703, 0.2701, 0.2703, 0.2701)]
        public void CheckZeroMeasurement_Exactly_One_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(distribution, ThresholdMock);
            Assert.False(zeroMeasurement);
        }
        
        /*
          Data inspired by distribution of benchmark
        
          [MethodImpl(MethodImplOptions.NoOptimization)]  
          public double Return1()
          {
              return 1;
          }
        
          with Intel Core i5-2500 CPU 3.30GHz (Sandy Bridge) CPU 
          and RyuJit x64
        */
        [Theory]
        [InlineData(0d, 0d, 0.00191d, 0.00530d, 0.00820d, 0.01383d, 0.01617d, 0.02183d, 0.02421d, 0.03640d, 0.03726d, 0.04894d, 0.05122d, 0.05924d, 0.06183d)]
        [InlineData(0d, 0d, 0d, 0d, 0d, 0d, 0d, 0.00138d, 0.00482d, 0.00616d, 0.01318d, 0.02266d, 0.03048d, 0.03144d)]
        [InlineData(0.02203d, 0.02523d, 0.02567d, 0.02706d, 0.03048d, 0.03461d, 0.03953d, 0.04127d, 0.04396d, 0.04939d, 0.05361d, 0.05670d, 0.06394d, 0.06812d, 0.06901d)]
        public void CheckZeroMeasurement_Less_Than_One_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(distribution, ThresholdMock);
            Assert.True(zeroMeasurement);
        }
        
        /*
          Sometimes appears distributions with all zero values
          That's definitely zero measurement
        */
        [Theory]
        [InlineData(0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d)]
        public void CheckZeroMeasurement_Exactly_Zero_ns_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(distribution, ThresholdMock);
            Assert.True(zeroMeasurement);
        }

        
        /*
          Test distributions inspired by data from method
           
          public double Abs() => Math.Abs(A);
                   
          with Intel Core i5-2500 CPU 3.30GHz (Sandy Bridge) CPU 
          and RyuJit x64
        */
        [Theory]
        [InlineData(0.57079d, 0.57469d, 0.57990d, 0.58025d, 0.58532d, 0.59250d, 0.59442d, 0.59487d, 0.59522d, 0.59619d, 0.59756d, 0.59802d, 0.60120d, 0.60813d, 0.61592d)]
        [InlineData(0.57106d, 0.57168d, 0.57326d, 0.57587d, 0.57958d, 0.58982d, 0.59493d, 0.59950d, 0.61413d, 0.62000d, 0.62607d, 0.63209d, 0.63730d, 0.65048d, 0.65119d)]
        [InlineData(0.57347d, 0.57483d, 0.57598d, 0.57681d, 0.57724d, 0.57906d, 0.57944d, 0.58182d, 0.58261d, 0.58300d, 0.58468d, 0.59045d, 0.59217d)]
        public void CheckZeroMeasurement_Around_Two_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurement(distribution, ThresholdMock);
            Assert.False(zeroMeasurement);
        }
    }
}