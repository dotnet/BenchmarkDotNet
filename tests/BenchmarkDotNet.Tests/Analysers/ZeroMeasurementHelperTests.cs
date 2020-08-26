using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using Xunit;

namespace BenchmarkDotNet.Tests.Analysers
{
    public class ZeroMeasurementHelperTests
    {
        #region OneSampleTests

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
        public void OneSample_Around_One_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurementOneSample(distribution, ThresholdMock);
            Assert.False(zeroMeasurement);
        }

        [Theory]
        [InlineData(0.2703, 0.2701, 0.2703, 0.2701, 0.2703, 0.2701, 0.2703, 0.2701, 0.2703, 0.2701, 0.2703, 0.2701)]
        public void OneSample_Exactly_One_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurementOneSample(distribution, ThresholdMock);
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
        public void OneSample_Less_Than_One_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurementOneSample(distribution, ThresholdMock);
            Assert.True(zeroMeasurement);
        }

        /*
          Sometimes appears distributions with all zero values
          That's definitely zero measurement
        */
        [Theory]
        [InlineData(0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d)]
        public void OneSample_Exactly_Zero_ns_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurementOneSample(distribution, ThresholdMock);
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
        public void OneSample_Around_Two_CPU_Cycle_Method(params double[] distribution)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurementOneSample(distribution, ThresholdMock);
            Assert.False(zeroMeasurement);
        }

        #endregion

        #region TwoSamplesTests
        /*
          Data inspired by distribution of benchmark (Workload Actual and Overhead Actual measurements)

          [MethodImpl(MethodImplOptions.NoOptimization)]
          public double Return1()
          {
              return 1;
          }

          with Intel Core i5-2500 CPU 3.30GHz (Sandy Bridge) CPU
          and RyuJit x64
        */
        public static IEnumerable<object[]> LessThanOneCycleTwoSamples
        {
            get
            {
                return new[]
                {
                    new object[] { new[] { 2.0037, 2.0062, 2.0066, 2.0073, 2.0089, 2.0103, 2.013, 2.0169, 2.0197, 2.0224, 2.0243, 2.0271, 2.0281, 2.0514, 2.0517 },
                                   new[] { 2.0426, 2.046, 2.0471, 2.0506, 2.0508, 2.0555, 2.0573, 2.0653, 2.0657, 2.0659, 2.0692, 2.0717, 2.0777, 2.0856, 2.0868 } },

                    new object[] { new[] { 2.0186, 2.0196, 2.0207, 2.0208, 2.0208, 2.0211, 2.0213, 2.0215, 2.0288, 2.0315, 2.0326, 2.039, 2.049, 2.055, 2.0598 },
                                   new[] { 2.0151, 2.0192, 2.0226, 2.0248, 2.0271, 2.0276, 2.0298, 2.0339, 2.0411, 2.0429, 2.0458, 2.0501, 2.061, 2.0733, 2.0744 } },

                    new object[] { new[] { 2.0049, 2.0141, 2.0194, 2.0253, 2.0264, 2.0296, 2.0333, 2.0422, 2.0438, 2.044, 2.047, 2.048, 2.0494, 2.0549, 2.0675 },
                                   new[] { 1.9963, 2.0037, 2.0037, 2.0046, 2.0051, 2.007, 2.0166, 2.021, 2.0225, 2.0247, 2.0391, 2.0473, 2.0572, 2.0576, 2.0582 } }
                };
            }
        }

        [Theory]
        [MemberData(nameof(LessThanOneCycleTwoSamples))]
        public void TwoSamples_Less_Than_One_CPU_Cycle_Method(double[] workload, double[] overhead)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workload, overhead);
            Assert.True(zeroMeasurement);
        }

        /*
          Test distributions inspired by data (Workload Actual and Overhead Actual) from method

          public double Sqrt14()
               => Math.Sqrt(1) + Math.Sqrt(2) + Math.Sqrt(3) + Math.Sqrt(4) +
                  Math.Sqrt(5) + Math.Sqrt(6) + Math.Sqrt(7) + Math.Sqrt(8) +
                  Math.Sqrt(9) + Math.Sqrt(10) + Math.Sqrt(11) + Math.Sqrt(12) +
                  Math.Sqrt(13) + Math.Sqrt(14);

          with Intel Core i5-2500 CPU 3.30GHz (Sandy Bridge) CPU
          and RyuJit x64
        */
        public static IEnumerable<object[]> OneCycleTwoSamples
        {
            get
            {
                return new[]
                {
                    new object[] { new[] { 2.34763, 2.34861, 2.34872, 2.34953, 2.35002, 2.35614, 2.35650, 2.36323, 2.36941, 2.37376, 2.38491, 2.38619, 2.38657, 2.38902, 2.39455 },
                                   new[] { 2.05899, 2.06069, 2.06243, 2.06405, 2.06762, 2.06785, 2.06889, 2.06891, 2.06895, 2.07531, 2.08003, 2.08024, 2.08342, 2.08959 } },

                    new object[] { new[] { 2.36960, 2.37438, 2.37442, 2.38332, 2.38940, 2.39099, 2.39394, 2.39974, 2.40808, 2.41760, 2.41980, 2.42275, 2.42828, 2.42946, 2.43763 },
                                   new[] { 2.06486, 2.06599, 2.07205, 2.07660, 2.07810, 2.07841, 2.08107, 2.08714, 2.10467, 2.10469, 2.11713, 2.12078, 2.12476, 2.12858, 2.13760 } },

                    new object[] { new[] { 2.35046, 2.35630, 2.35788, 2.35801, 2.36632, 2.36841, 2.36925, 2.36980, 2.36998, 2.37153, 2.37330, 2.38491, 2.38732, 2.38853, 2.41052 },
                                   new[] { 2.06291, 2.06545, 2.06763, 2.07381, 2.07568, 2.07810, 2.07894, 2.08153, 2.08264, 2.09000, 2.09814, 2.10082, 2.10107, 2.10576, 2.12841 } }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OneCycleTwoSamples))]
        public void TwoSamples_Around_One_CPU_Cycle_Method(double[] workload, double[] overhead)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workload, overhead);
            Assert.False(zeroMeasurement);
        }

        /*
          Test distributions inspired by data (Workload Actual and Overhead Actual) from method

          public double Abs() => Math.Abs(A);

          with Intel Core i5-2500 CPU 3.30GHz (Sandy Bridge) CPU
          and RyuJit x64
        */
        public static IEnumerable<object[]> TwoCycleTwoSamples
        {
            get
            {
                return new[]
                {
                    new object[] { new[] { 2.6561, 2.658, 2.6606, 2.6621, 2.6636, 2.6639, 2.6656, 2.6673, 2.6741, 2.6754, 2.6787, 2.6935, 2.6997, 2.7313, 2.7394 },
                                   new[] { 2.0387, 2.0436, 2.0485, 2.0485, 2.0525, 2.0584, 2.0638, 2.0678, 2.0678, 2.0698, 2.0703, 2.0715, 2.0825, 2.0864 } },

                    new object[] { new[] { 2.6368, 2.6504, 2.652, 2.6541, 2.6607, 2.6642, 2.6698, 2.6749, 2.679, 2.6847, 2.6858, 2.6883, 2.6929, 2.702, 2.7134 },
                                   new[] { 2.04, 2.0461, 2.0481, 2.0485, 2.0502, 2.0523, 2.0547, 2.0602, 2.0613, 2.0677, 2.069, 2.0691, 2.0699, 2.0865 } },

                    new object[] { new[] { 2.6607, 2.6691, 2.67, 2.6741, 2.6753, 2.679, 2.6856, 2.6869, 2.6893, 2.692, 2.6971, 2.7146, 2.7245, 2.7368 },
                                   new[] { 2.0346, 2.054, 2.0655, 2.0673, 2.0718, 2.0748, 2.0766, 2.0839, 2.0856, 2.0869, 2.0924, 2.0968, 2.1129, 2.1148, 2.1328 } }
                };
            }
        }

        [Theory]
        [MemberData(nameof(TwoCycleTwoSamples))]
        public void TwoSamples_Around_Two_CPU_Cycle_Method(double[] workload, double[] overhead)
        {
            var zeroMeasurement = ZeroMeasurementHelper.CheckZeroMeasurementTwoSamples(workload, overhead);
            Assert.False(zeroMeasurement);
        }
        #endregion
    }
}