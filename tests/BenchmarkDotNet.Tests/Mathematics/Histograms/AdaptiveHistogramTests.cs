﻿using System;
using BenchmarkDotNet.Mathematics.Histograms;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mathematics.Histograms
{
    public class AdaptiveHistogramTests
    {
        private readonly ITestOutputHelper output;

        public AdaptiveHistogramTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void TrivialTest1()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 1.0, 2.0, 3.0, 4.0, 5.0 },
                1,
                new[]
                {
                    new[] { 1.0 },
                    new[] { 2.0 },
                    new[] { 3.0 },
                    new[] { 4.0 },
                    new[] { 5.0 }
                });
        }

        [Fact]
        public void TrivialTest2()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 1.0, 2.0, 3.0, 4.0, 5.0 },
                2.5,
                new[]
                {
                    new[] { 1.0, 2.0, 3.0 },
                    new[] { 4.0, 5.0 }
                });
        }

        [Fact]
        public void TrivialTest3()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 2.7 },
                2.0,
                new[]
                {
                    new[] { 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 2.7 }
                });
        }

        [Fact]
        public void TrivialTest4()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 0.0, 0.0053448932245373729, 0.0074317916482686992 },
                0.0046509790766586616,
                new[]
                {
                    new[] { 0.0 },
                    new[] { 0.0053448932245373729, 0.0074317916482686992 }
                });
        }

        [Fact]
        public void AutoTrivialTest1()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 6.9550, 6.9743, 7.0469 },
                new[]
                {
                    new[] { 6.9550, 6.9743, 7.0469 }
                });
        }


        [Fact]
        public void AutoTrivialTest2()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 0.0, 0.0, 0.0 },
                new[]
                {
                    new[] { 0.0, 0.0, 0.0 }
                });
        }

        [Fact]
        public void AutoTrivialTest3()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 0.00000001, 0.00000002, 0.00000003 },
                new[]
                {
                    new[] { 0.00000001, 0.00000002, 0.00000003 }
                });
        }

        [Fact]
        public void AutoTrivialTest4()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 0.00000001, 0.00000002, 0.00000003, 1.00000001, 1.00000002, 1.00000003 },
                new[]
                {
                    new[] { 0.00000001, 0.00000002, 0.00000003 },
                    Array.Empty<double>(),
                    new[] { 1.00000001, 1.00000002, 1.00000003 }
                });
        }

        [Fact]
        public void AutoSyntheticTestTrimodal()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[] { 1.0, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3 },
                new[]
                {
                    new[] { 1.0, 1, 1, 1 },
                    Array.Empty<double>(),
                    new[] { 2.0, 2, 2, 2 },
                    Array.Empty<double>(),
                    new[] { 3.0, 3, 3, 3 }
                });
        }

        [Fact]
        public void AutoTestUnimodal1()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[]
                {
                    54.1166, 54.4516, 53.1706, 54.5196, 51.9886, 54.4686, 52.9636, 52.9576, 54.3516, 53.7846, 53.6686, 51.8006, 53.5406, 53.6156, 53.4546
                },
                new[]
                {
                    true,
                    true
                });
        }

        [Fact]
        public void AutoTestUnimodal2()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[]
                {
                    33.5455, 32.2774, 33.6649, 35.3296, 31.6737, 31.6836, 31.7396, 31.7001, 33.6881, 30.6352, 30.6761, 32.5385, 31.1269, 33.0186, 31.7488,
                    30.9851, 30.7059, 31.5718, 32.9712, 31.3161, 30.5636, 30.6786, 31.1183, 30.9629, 31.4356, 33.9919, 31.4916, 30.5728, 31.6186, 30.8011,
                    30.8926, 30.2854, 30.2282, 30.5786, 30.6909, 30.2862, 30.8433, 31.8990, 32.1198, 31.5409, 31.7843, 30.4518, 34.7756, 31.1336, 32.5714,
                    32.2337
                },
                new[]
                {
                    true,
                    true,
                    true,
                    true,
                    true
                });
        }

        [Fact]
        public void AutoTestUnimodal3()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[]
                {
                    24.8822, 24.8288, 24.8050, 24.7990, 24.8510, 24.8993, 24.8376, 24.8345, 24.9441, 24.9758, 25.0026, 24.8308
                },
                new[]
                {
                    true
                });
        }

        [Fact]
        public void AutoTestBimodal()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[]
                {
                    53.5559, 53.6549, 101.8009, 53.6549, 54.3409, 100.9439, 103.3839, 54.1319, 103.8739, 54.4979, 53.9889, 104.2749, 53.1299, 54.2599, 54.4389,
                    101.7749, 52.2869, 103.0219, 103.0079, 54.9709, 54.5199, 52.8009, 104.3999, 104.7339, 102.5139, 54.1979, 102.3789, 54.9309, 103.6629,
                    104.1529, 104.1629, 53.8849, 52.2119, 53.7939, 103.8419, 101.2259, 54.8619, 101.3639, 100.7259, 104.2899, 52.1469, 54.8179, 50.0849,
                    52.8589, 103.5959, 105.1049, 54.9159, 52.1189, 103.0839, 53.2739, 103.0819, 54.6049, 53.7459, 102.8169, 103.8609, 53.8309, 53.2619,
                    103.9679, 101.7129, 50.1929, 54.2469, 54.4669, 54.6009, 54.3319, 101.7119, 53.5829, 52.2749, 53.7199, 104.1699, 54.7329, 100.8459, 53.7399,
                    53.1539, 104.8099, 53.2049, 54.2959, 50.8879, 104.5729, 51.0259, 103.6519, 103.6909, 54.5209, 51.7519, 101.5849, 54.9299, 54.1139, 53.4199,
                    55.0609, 104.4749, 104.0589, 53.1139, 54.3649, 103.9969, 104.9919, 101.0799, 53.5399, 101.3219, 101.7309, 51.0129, 104.1249,
                },
                new[]
                {
                    true, false, false, false, false,
                    true
                });
        }

        [Fact]
        public void AutoTestTrimodal()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[]
                {
                    53.8268, 54.9398, 103.4098, 53.5118, 52.1718, 154.0758, 103.4258, 54.5178, 153.9198, 54.7608, 52.1608, 103.1478, 54.2528, 104.9618, 54.9458,
                    104.8898, 53.9168, 153.1668, 104.9088, 105.0408, 53.7018, 53.6178, 153.7938, 154.3638, 104.6428, 55.0258, 152.6418, 54.1728, 152.4818,
                    154.9568, 102.4368, 53.8458, 50.5228, 54.9138, 103.9068, 154.3708, 53.8648, 103.1468, 154.1558, 153.5758, 103.9948, 53.2298, 54.5768,
                    53.3598, 153.4818, 102.8688, 54.3768, 54.3298, 153.0418, 54.0498, 101.2148, 54.6768, 53.2578, 104.8848, 101.6078, 53.6078, 104.3758,
                    153.3428, 105.0318, 54.4548, 53.1688, 105.0628, 51.7798, 52.6878, 152.5438, 54.8058, 103.9618, 53.2448, 153.4528, 103.6548, 103.3058,
                    104.7568, 52.8858, 153.3038, 51.9488, 54.3588, 52.6368, 103.5778, 53.1848, 153.8708, 154.9128, 103.9158, 104.4708, 104.9618, 53.2018,
                    103.7408, 51.8718, 104.8708, 153.6998, 103.3718, 103.2448, 54.1768, 154.5538, 101.7978, 153.8588, 51.9978, 152.5858, 154.6038, 53.6288,
                    153.7528,
                },
                new[]
                {
                    true, false, false,
                    true, false, false,
                    true
                });
        }

        [Fact]
        public void AutoTestQuadrimodal1()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[]
                {
                    54.4140, 54.8640, 151.6690, 54.7780, 104.6170, 154.8150, 150.3550, 51.7410, 203.5600, 52.9680, 102.1660, 155.1540, 101.4900, 105.0790,
                    104.8150, 154.0340, 54.5340, 205.1420, 155.0590, 103.1720, 50.8400, 54.5400, 150.3000, 201.8370, 153.4970, 53.3890, 153.5630, 53.6010,
                    202.4930, 154.6960, 153.3370, 54.6520, 53.6120, 103.7030, 151.6260, 203.8850, 50.6300, 155.1610, 203.0760, 153.8910, 104.9950, 54.6320,
                    54.6240, 54.2760, 202.4930, 154.8500, 50.7690, 51.9110, 203.8590, 102.5350, 154.8540, 50.1520, 54.6250, 152.8100, 154.8730, 55.0540,
                    101.2380, 151.5090, 154.6420, 55.0550, 54.5840, 104.1540, 100.6920, 53.9030, 153.8870, 53.5660, 101.3580, 51.4430, 204.0370, 101.4220,
                    153.5490, 102.1340, 54.6950, 204.0110, 53.1390, 53.1830, 52.9660, 153.8270, 52.4380, 202.6190, 202.6890, 100.2270, 105.0480, 150.1560,
                    53.5010, 102.9840, 53.9720, 104.0280, 204.0530, 153.1220, 103.0630, 103.7960, 203.6950, 154.0190, 151.5000, 54.3830, 203.1370, 202.2610,
                    103.6030, 152.8300
                },
                new[]
                {
                    true, false,
                    true, false,
                    true, false,
                    true
                });
        }

        [Fact]
        public void AutoTestQuadrimodal2()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive,
                new[]
                {
                    52.9868, 53.7488, 152.8768, 50.8788, 102.7788, 155.1378, 153.6468, 52.8868, 201.8688, 53.8768, 103.4568, 153.5178, 102.9078, 104.0128,
                    104.1018, 153.3768, 51.0948, 203.6908, 153.9508, 103.0448, 54.5758, 53.3828, 152.4038, 204.6468, 153.3098, 53.0028, 153.3958, 54.5438,
                    203.6268, 151.8578, 154.7498, 53.4388, 54.9368, 103.9388, 154.9488, 204.9338, 54.5718, 153.8728, 205.0888, 154.4578, 103.7568, 53.7358,
                    52.5138, 52.6698, 201.0788, 153.3238, 54.7338, 55.0328, 204.6378, 104.8608, 151.4808, 51.0258, 54.1618, 154.9518, 151.5528, 54.7568,
                    104.2438, 154.8118, 152.9708, 53.6878, 54.5128, 102.5938, 105.0188, 51.5798, 153.3078, 53.1978, 103.2718, 53.0328, 203.1028, 104.9488,
                    153.6708, 105.1028, 54.4878, 202.5518, 50.2618, 51.9998, 54.6478, 153.9048, 53.8258, 203.7968, 204.0508, 102.5758, 103.1168, 153.1998,
                    53.0008, 103.1648, 52.8238, 104.6748, 204.2478, 154.6658, 103.0448, 103.4738, 203.2368, 154.0128, 151.6948, 54.6178, 200.8398, 203.0058,
                    104.8248, 153.7678
                },
                new[]
                {
                    true, false,
                    true, false,
                    true, false,
                    true
                });
        }

        /// <summary>
        /// <see cref="https://github.com/dotnet/BenchmarkDotNet/issues/802"/>
        /// </summary>
        [Fact]
        public void Issue802()
        {
            var values = new[]
            {
                0.005203099569,
                0.005206927083,
                0.005208544515,
                0.005213252157,
                0.005220535482,
                0.005223894246,
                0.005225830282,
                0.005232740072,
                0.005235999349,
                0.005237509969,
                0.005241159871,
                0.005241720174,
                0.005243189901,
                0.005251286214,
                0.005253464559
            };
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive, values, 0.0001, new[] { values });
        }
        
        /// <summary>
        /// <see cref="https://github.com/dotnet/BenchmarkDotNet/issues/870"/>
        /// </summary>
        [Fact]
        public void Issue870()
        {
            var values = new[]
            {
                0.003599657069,
                0.003634646485,
                0.003697427313,
                0.003699466536,
                0.003708548049,
                0.003713960151,
                0.003734243611,
                0.003739674340,
                0.003744175980,
                0.003800773571,
                0.003830879132,
                0.003877980957,
                0.003890512834,
                0.003932129542,
                0.003952739338
            };
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Adaptive, values, 0.0001,
                new[]
                {
                    new[] { values[0] },
                    new[] { values[1], values[2], values[3] },
                    new[] { values[4], values[5], values[6], values[7], values[8] },
                    new[] { values[9], values[10], values[11], values[12] },
                    new[] { values[13], values[14] }
                });
        }
    }
}