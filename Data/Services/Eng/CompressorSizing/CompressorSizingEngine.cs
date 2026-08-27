#nullable disable
using System.Globalization;
using System.IO;

namespace Data.Services.Eng.CompressorSizing
{
    public class CompressorSizingFunction
    {
        public double UnitConvertor(string A, string B, double x)
        {
            double y = 0, t = 0;
            if (A == "C")
            {
                t = x + 273.15;
            }
            else if (A == "K")
            {
                t = x;
            }
            else if (A == "atm")
            {
                t = x * 1.01325;
            }
            else if (A == "bara")
            {
                t = x;
            }

            if (B == "C")
            {
                y = t - 273.15;
            }
            else if (B == "K")
            {
                y = t;
            }
            else if (B == "atm")
            {
                y = t / 1.01325;
            }
            else if (B == "bara")
            {
                y = t;
            }

            return y;
        }
        public double FlowCalculator(string A, string B, double q, double P, double T, double H)
        {
            double f = 0, t = 0;

            double C1 = -8.391384e-10;
            double C2 = 2.942852e-6;
            double C3 = 2.598317e-4;
            double C4 = 2.667707e-2;
            double C5 = 1.422996;
            double C6 = 4.362130e1;
            double C7 = 6.047532e2;

            T -= 273.15;
            double Pws = (C1 * Math.Pow(T, 6) + C2 * Math.Pow(T, 5) + C3 * Math.Pow(T, 4) + C4 * Math.Pow(T, 3) + C5 * Math.Pow(T, 2) + C6 * T + C7) / 100000.0;
            double Mw = 18.015;
            double Ma = 28.97;
            double Ru = 8.3144;
            double Pw = (H / 100.0) * Pws;
            double W = (Mw / Ma) * (Pw / (P - Pw));
            double Xa = 1.0 / (1.0 + W * (Ma / Mw));
            double Xw = W / ((Mw / Ma) + W);
            double Mm = Xa * Ma + Xw * Mw;
            double Rm = 1000.0 * Ru / Mm;
            double rho = (P * 100000.0) / (Rm * (T + 273.15));

            double Pnorm = 1.01325;
            double Tnorm = 273.15;

            T += 273.15;
            double Xs = (T / Tnorm) * (Pnorm / (P - (H / 100) * Pws));

            if (A == "Im^3/h")
            {
                t = q * rho;
            }
            if (A == "Nm^3/h")
            {
                t = q * Xs * rho;
            }
            if (A == "kg/h")
            {
                t = q;
            }

            if (B == "Im^3/h")
            {
                f = t / rho;
            }
            if (B == "Nm^3/h")
            {
                f = t / Xs / rho;
            }
            if (B == "kg/h")
            {
                f = t;
            }

            return f;
        }
        public double Wcalculator(double P, double T, double H)
        {
            double C1 = -8.391384e-10;
            double C2 = 2.942852e-6;
            double C3 = 2.598317e-4;
            double C4 = 2.667707e-2;
            double C5 = 1.422996;
            double C6 = 4.362130e1;
            double C7 = 6.047532e2;
            T -= 273.15;
            double Pws = (C1 * Math.Pow(T, 6) + C2 * Math.Pow(T, 5) + C3 * Math.Pow(T, 4) + C4 * Math.Pow(T, 3) + C5 * Math.Pow(T, 2) + C6 * T + C7) / 100000.0;
            double Mw = 18.015;
            double Ma = 28.97;
            double Pw = (H / 100.0) * Pws;
            double W = (Mw / Ma) * (Pw / (P - Pw));
            return W;
        }
        public double[] Polyfit(double[] x, double[] y, int n)
        {
            double[] X, Y, a;
            double[,] B;

            X = new double[2 * n + 1];
            Y = new double[n + 1];
            a = new double[n + 1];
            B = new double[n + 1, n + 2];

            int N = x.Length;

            for (int i = 0; i < 2 * n + 1; i++)
            {
                X[i] = 0.0;
                for (int j = 0; j < N; j++)
                {
                    X[i] = X[i] + Math.Pow(x[j], i);
                }
            }

            for (int i = 0; i <= n; i++)
            {
                for (int j = 0; j <= n; j++)
                {
                    B[i, j] = X[i + j];
                }
            }

            for (int i = 0; i < n + 1; i++)
            {
                Y[i] = 0;
                for (int j = 0; j < N; j++)
                {
                    Y[i] = Y[i] + Math.Pow(x[j], i) * y[j];
                }
            }

            for (int i = 0; i <= n; i++)
            {
                B[i, n + 1] = Y[i];
            }

            n += 1;

            for (int i = 0; i < n; i++)
            {
                for (int k = i + 1; k < n; k++)
                {
                    if (B[i, i] < B[k, i])
                    {
                        for (int j = 0; j <= n; j++)
                        {
                            double temp = B[i, j];
                            B[i, j] = B[k, j];
                            B[k, j] = temp;
                        }
                    }
                }
            }

            for (int i = 0; i < n - 1; i++)
            {
                for (int k = i + 1; k < n; k++)
                {
                    double t = B[k, i] / B[i, i];
                    for (int j = 0; j <= n; j++)
                    {
                        B[k, j] = B[k, j] - t * B[i, j];
                    }
                }
            }

            for (int i = n - 1; i >= 0; i--)
            {
                a[i] = B[i, n];
                for (int j = 0; j < n; j++)
                    if (j != i)
                        a[i] = a[i] - B[i, j] * a[j];
                a[i] = a[i] / B[i, i];
            }

            return a;
        }
        public double Polyeval(double[] p, double x)
        {
            int N = p.Length;
            double y = 0;
            for (int i = 0; i < N; i++)
            {
                y += p[i] * Math.Pow(x, i);
            }

            return y;
        }
        public double[] Linspace(double a, double b, int n)
        {
            double[] x;
            x = new double[n];
            double d = (b - a) / ((double)n - 1.0);
            for (int i = 0; i < n; i++)
            {
                x[i] = a + i * d;
            }

            return x;
        }
        public double PlinearInterpolation(double[] x, double[] y, double xi)
        {
            double yi = 0;
            int N = x.Length;
            if (xi == x[0])
            {
                yi = y[0];
            }
            for (int i = 0; i < (N - 1); i++)
            {
                if ((xi > x[i]) & (xi <= x[i + 1]))
                {
                    yi = y[i] + ((y[i + 1] - y[i]) / (x[i + 1] - x[i])) * (xi - x[i]);
                }
            }
            return yi;
        }
    }
    public class InputDataType
    {
        public string ProjectName { get; set; }
        public string Custormer { get; set; }
        public string Contact { get; set; }
        public DateTime Date = DateTime.Now;
        public double StdPressure { get; set; }
        public double StdTemperature { get; set; }
        public double StdRelativeHumidity { get; set; }
        public string StdPressure_u { get; set; }
        public string StdTemperature_u { get; set; }
        public double Pin { get; set; }
        public double dPin { get; set; }
        public double Tin { get; set; }
        public double Hin { get; set; }
        public double Q { get; set; }
        public double Pd { get; set; }
        public string Pin_u { get; set; }
        public string dPin_u { get; set; }
        public string Tin_u { get; set; }
        public string Q_u { get; set; }
        public string Pd_u { get; set; }
        public double Tcw_in { get; set; }
        public double Tcw_rise { get; set; }
        public string modelName { get; set; }
    };
    public class CompressorReportType
    {
        public string modelName;
        public double Pinlet;
        public double[] p_IGV30 = new double[20];
        public double[] p_IGV70 = new double[20];
        public double[] p_IGV100 = new double[20];
        public double[] p_TurnDown = new double[4];
        public double[] p_SurgeLine = new double[4];
        public double[] p_DP = new double[2];
        public double[] p_Border = new double[4];
        public double DTR = 0, RTR = 0, CPF = 0;
        public double MaxF, MaxP;
        public double[] k_IGV30 = new double[20];
        public double[] k_IGV70 = new double[20];
        public double[] k_IGV100 = new double[20];
        public double[] k_TurnDown = new double[4];
        public double[] k_DP = new double[2];
        public double[] k_Border = new double[4];
        public double mic, mac, moc, mtot;
        public double dp_mic, dp_mac, dp_moc;
        public double wTrise;
        public double Te3;
        public double OilPumpKW;
        public double OilHeater;
        public double Sst1, Sst2, Sst3;
        public double Dinlet, Ddischarge, Dblowoff, Doutlet;
        public string Q_u;
        public string Pd_u;

        public string DeliveredFlow;
        public string InletCoolingWaterTemp;
        public string Pressure;
        public string Temprature;
        public string Pressure2;
        
        
        public string DesignedTurnDownRatio;
        public string RequiredTurnDownRatio;
        public string OperatingPressure;
        public string CompressorDischargeTemperature;
        public string TemperatureAtAfterCoolerOutlet;
        public string MaximumInletFlow;
        public string CouplingPowerAtMaximumInletFlow;
        public string InterCoolersConsumption;
        public string InterCoolerPressureDrop;
        public string AfterCoolerConsumption;
        public string AfterCoolerPressureDrop;
        public string OilCoolerConsumption;
        public string OilCoolerPressureDrop;
        public string TotalCoolingWaterConsumption;
        public string CoolingWaterMaximumTemperatureRise;
    };

    public class CompressorProducts
    {
        private readonly string[] listofCompressors;
        private readonly double[][] compStdCurves;
        private readonly double[][] compPwrCurves;
        private readonly double[][] cwrequirement;

        private readonly CompressorSizingFunction Func = new CompressorSizingFunction();

        public CompressorProducts(string dataDirectory)
        {
            if (string.IsNullOrWhiteSpace(dataDirectory) || !Directory.Exists(dataDirectory))
                throw new FileNotFoundException("Compressor sizing data directory was not found.", dataDirectory);

            var curvesPath = Path.Combine(dataDirectory, "compCurves");
            var powersPath = Path.Combine(dataDirectory, "compPowers");
            var coolingPath = Path.Combine(dataDirectory, "CWdata");
            if (!File.Exists(curvesPath) || !File.Exists(powersPath) || !File.Exists(coolingPath))
                throw new FileNotFoundException(
                    "Compressor curve files (compCurves, compPowers, CWdata) were not found in " + dataDirectory);

            listofCompressors = new string[36]
            {
                "HYTC400-1"  ,  "HYTC400-2"  ,  "HYTC400-3"  ,  "HYTC400-4"  ,
                "HYTC500-1"  ,  "HYTC500-2"  ,  "HYTC500-3"  ,  "HYTC500-4"  ,
                "HYTC600-1"  ,  "HYTC600-2"  ,  "HYTC600-3"  ,  "HYTC600-4"  ,
                "HYTB700-1"  ,  "HYTB700-2"  ,  "HYTB700-3"  ,  "HYTB700-4"  ,
                "HYTB800-1"  ,  "HYTB800-2"  ,  "HYTB800-3"  ,  "HYTB800-4"  ,
                "HYTB900-1"  ,  "HYTB900-2"  ,  "HYTB900-3"  ,  "HYTB900-4"  ,
                "HYTA1000-1" ,  "HYTA1000-2" ,  "HYTA1000-3" ,  "HYTA1000-4" ,
                "HYTA1250-1" ,  "HYTA1250-2" ,  "HYTA1250-3" ,  "HYTA1250-4" ,
                "HYTA1500-1" ,  "HYTA1500-2" ,  "HYTA1500-3" ,  "HYTA1500-4" ,
            };

            compStdCurves = new double[72][];
            for (int i = 0; i < 72; i++)
            {
                compStdCurves[i] = new double[30];
            }
            compPwrCurves = new double[72][];
            for (int i = 0; i < 72; i++)
            {
                compPwrCurves[i] = new double[30];
            }
            cwrequirement = new double[5][];
            for (int i = 0; i < 5; i++)
            {
                cwrequirement[i] = new double[180];
            }

            using (var sr1 = new StreamReader(curvesPath))
            {
                for (int k = 0; k < 4; k++)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        string[] line = ReadRequiredLine(sr1, curvesPath).Split('\t');
                        for (int j = 0; j < 18; j++)
                        {
                            compStdCurves[18 * k + j][i] = ParseNumber(line[j]);
                            if ((j % 2) == 0)
                            {
                                compStdCurves[18 * k + j][i] /= 3600.0;
                            }
                        }
                    }
                }
            }

            using (var sr2 = new StreamReader(powersPath))
            {
                for (int k = 0; k < 4; k++)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        string[] line = ReadRequiredLine(sr2, powersPath).Split('\t');
                        for (int j = 0; j < 18; j++)
                        {
                            compPwrCurves[18 * k + j][i] = ParseNumber(line[j]);
                            if ((j % 2) == 0)
                            {
                                compPwrCurves[18 * k + j][i] /= 3600.0;
                            }
                        }
                    }
                }
            }

            using (var sr3 = new StreamReader(coolingPath))
            {
                for (int k = 0; k < 180; k++)
                {
                    string[] line = ReadRequiredLine(sr3, coolingPath).Split('\t');
                    for (int i = 0; i < 5; i++)
                    {
                        cwrequirement[i][k] = ParseNumber(line[i]);
                    }
                }
            }

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    double[] ptr1 = compStdCurves[18 * j + (2 * i)];
                    double[] ptr2 = compStdCurves[18 * j + (2 * i + 1)];
                    double[] ptr3 = compPwrCurves[18 * j + (2 * i)];
                    double[] ptr4 = compPwrCurves[18 * j + (2 * i + 1)];

                    double[] ix1 = new double[10];
                    Array.ConstrainedCopy(ptr1, 0, ix1, 0, 10);
                    double[] ix2 = new double[10];
                    Array.ConstrainedCopy(ptr1, 10, ix2, 0, 10);
                    double[] ix3 = new double[10];
                    Array.ConstrainedCopy(ptr1, 20, ix3, 0, 10);
                    double[] ix4 = new double[10];
                    Array.ConstrainedCopy(ptr3, 0, ix4, 0, 10);
                    double[] ix5 = new double[10];
                    Array.ConstrainedCopy(ptr3, 10, ix5, 0, 10);
                    double[] ix6 = new double[10];
                    Array.ConstrainedCopy(ptr3, 20, ix6, 0, 10);

                    double[] iy1 = new double[10];
                    Array.ConstrainedCopy(ptr2, 0, iy1, 0, 10);
                    double[] iy2 = new double[10];
                    Array.ConstrainedCopy(ptr2, 10, iy2, 0, 10);
                    double[] iy3 = new double[10];
                    Array.ConstrainedCopy(ptr2, 20, iy3, 0, 10);
                    double[] iy4 = new double[10];
                    Array.ConstrainedCopy(ptr4, 0, iy4, 0, 10);
                    double[] iy5 = new double[10];
                    Array.ConstrainedCopy(ptr4, 10, iy5, 0, 10);
                    double[] iy6 = new double[10];
                    Array.ConstrainedCopy(ptr4, 20, iy6, 0, 10);

                    double[] p4fit1 = Func.Polyfit(ix1, iy1, 4);
                    double[] p4fit2 = Func.Polyfit(ix2, iy2, 4);
                    double[] p4fit3 = Func.Polyfit(ix3, iy3, 4);
                    double[] p4fit4 = Func.Polyfit(ix4, iy4, 4);
                    double[] p4fit5 = Func.Polyfit(ix5, iy5, 4);
                    double[] p4fit6 = Func.Polyfit(ix6, iy6, 4);

                    double[] intv1 = Func.Linspace(ptr1[0], ptr1[9], 10);
                    double[] intv2 = Func.Linspace(ptr1[10], ptr1[19], 10);
                    double[] intv3 = Func.Linspace(ptr1[20], ptr1[29], 10);

                    for (int k = 0; k < 10; k++)
                    {
                        compStdCurves[18 * j + (2 * i)][k] = intv1[k];
                        compStdCurves[18 * j + (2 * i + 1)][k] = Func.Polyeval(p4fit1, intv1[k]);
                        compStdCurves[18 * j + (2 * i)][10 + k] = intv2[k];
                        compStdCurves[18 * j + (2 * i + 1)][10 + k] = Func.Polyeval(p4fit2, intv2[k]);
                        compStdCurves[18 * j + (2 * i)][20 + k] = intv3[k];
                        compStdCurves[18 * j + (2 * i + 1)][20 + k] = Func.Polyeval(p4fit3, intv3[k]);
                        compPwrCurves[18 * j + (2 * i)][k] = intv1[k];
                        compPwrCurves[18 * j + (2 * i + 1)][k] = Func.Polyeval(p4fit4, intv1[k]);
                        compPwrCurves[18 * j + (2 * i)][10 + k] = intv2[k];
                        compPwrCurves[18 * j + (2 * i + 1)][10 + k] = Func.Polyeval(p4fit5, intv2[k]);
                        compPwrCurves[18 * j + (2 * i)][20 + k] = intv3[k];
                        compPwrCurves[18 * j + (2 * i + 1)][20 + k] = Func.Polyeval(p4fit6, intv3[k]);
                    }
                }
            }
        }
        public string E_base_selection(InputDataType inCondition)
        {
            string selectedCompressor;
            double Pin = Func.UnitConvertor(inCondition.Pin_u, "bara", inCondition.Pin);
            double dPin = Func.UnitConvertor(inCondition.dPin_u, "bara", inCondition.dPin);
            Pin -= dPin;

            double Tin = Func.UnitConvertor(inCondition.Tin_u, "K", inCondition.Tin);
            double Hin = inCondition.Hin;
            double Q = Func.FlowCalculator(inCondition.Q_u, "kg/h", inCondition.Q, Pin + dPin, Tin, Hin) / 3600.0;
            double Pd = Func.UnitConvertor(inCondition.Pd_u, "bara", inCondition.Pd);

            double PR = Pd / Pin;

            int col = 0;

            if (PR > 12.0)
            {
                selectedCompressor = "4-Stage Required!!";
                return selectedCompressor;
            }
            else if (PR <= 12.0 && PR > 10.48)
            {
                col = 1;
            }
            else if (PR <= 10.48 && PR > 9.16)
            {
                col = 2;
            }
            else if (PR <= 9.16 && PR > 7.65)
            {
                col = 3;
            }
            else if (PR <= 7.65 && PR > 5.0)
            {
                col = 4;
            }
            else if (PR < 5.0)
            {
                selectedCompressor = "2-Stage Required!!";
                return selectedCompressor;
            }

            double[][] CC;
            CC = new double[18][];
            for (int i = 0; i < 18; i++)
            {
                CC[i] = new double[12];
            }

            for (int i = 0; i < 18; i++)
            {
                CC[i][0] = compStdCurves[18 * (col - 1) + i][0];
                CC[i][1] = compStdCurves[18 * (col - 1) + i][10];
                for (int j = 0; j < 10; j++)
                {
                    CC[i][j + 2] = compStdCurves[18 * (col - 1) + i][20 + j];
                }
            }

            double Tref = (35.0 + 273.15);
            double Pref = 0.972;
            double Tratio = Tin / Tref;
            double Pratio = Pin / Pref;

            double cX1 = -0.063276 * Math.Pow(Pratio, 2) + 1.139254 * Pratio - 0.075972;
            double cY1 = -0.071200 * Math.Pow(Pratio, 2) + 1.157932 * Pratio - 0.084129;
            double cX2 = -0.077294 * Math.Pow(Tratio, 2) + 1.072132 * Tratio + 0.005148;
            double cY2 = +0.138523 * Math.Pow(Tratio, 2) + 0.677078 * Tratio + 0.181374;

            double W = Func.Wcalculator(Pin, Tin, Hin);

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    CC[2 * i][j] *= (cX1 / cX2 * (1.0 + W) * (1.0 - 0.03 * (i / 10.0) * (Hin / 100.0)) * 0.985);
                    CC[2 * i + 1][j] *= (cY1 / cY2 * (1.0 - 0.02 * (Hin / 100.0)));
                }
            }

            double[] Uband = new double[9], Lband = new double[9];
            for (int i = 0; i < 9; i++)
            {
                double[] ptr1 = CC[2 * i];
                double[] ptr2 = CC[2 * i + 1];

                double[] SX = { ptr1[0], ptr1[1], ptr1[2] };
                double[] SY = { ptr2[0], ptr2[1], ptr2[2] };
                double[] bLfit = Func.Polyfit(SY, SX, 1);
                Lband[i] = Func.Polyeval(bLfit, Pd);

                double[] ix = new double[10];
                Array.ConstrainedCopy(ptr1, 2, ix, 0, 10);
                double[] iy = new double[10];
                Array.ConstrainedCopy(ptr2, 2, iy, 0, 10);
                double[] bUfit = Func.Polyfit(iy, ix, 4);
                Uband[i] = Func.Polyeval(bUfit, Pd);
            }

            int row = 0;
            if (Q <= Lband[0] || Q >= Uband[8])
            {
                selectedCompressor = "Flow Out of Range!!";
                return selectedCompressor;
            }
            else
            {
                for (int i = 0; i < 9; i++)
                {
                    if (Q < (Uband[i] * (1.055 - 0.005 * ((double)i + 1.0))))
                    {
                        row = i + 1;
                        break;
                    }
                }
            }

            int s = 4 * (row - 1) + (col - 1);
            selectedCompressor = listofCompressors[s];

            return selectedCompressor;
        }
        public string R_base_selection(InputDataType inCondition)
        {
            string selectedCompressor;

            double Pin = Func.UnitConvertor(inCondition.Pin_u, "bara", inCondition.Pin);
            double dPin = Func.UnitConvertor(inCondition.dPin_u, "bara", inCondition.dPin);
            Pin -= dPin;

            double Tin = Func.UnitConvertor(inCondition.Tin_u, "K", inCondition.Tin);
            double Hin = inCondition.Hin;
            double Q = Func.FlowCalculator(inCondition.Q_u, "kg/h", inCondition.Q, Pin + dPin, Tin, Hin) / 3600.0;
            double Pd = Func.UnitConvertor(inCondition.Pd_u, "bara", inCondition.Pd);

            double PR = Pd / Pin;

            int col = 0;

            if (PR > 12.0)
            {
                selectedCompressor = "4-Stage Required!!";
                return selectedCompressor;
            }
            else if (PR <= 12.0 && PR > 10.48)
            {
                col = 1;
            }
            else if (PR <= 10.48 && PR > 9.16)
            {
                col = 2;
            }
            else if (PR <= 9.16 && PR > 7.65)
            {
                col = 3;
            }
            else if (PR <= 7.65 && PR > 5.0)
            {
                col = 4;
            }
            else if (PR < 5.0)
            {
                selectedCompressor = "2-Stage Required!!";
                return selectedCompressor;
            }

            double[][] CC;
            CC = new double[18][];
            for (int i = 0; i < 18; i++)
            {
                CC[i] = new double[12];
            }

            for (int i = 0; i < 18; i++)
            {
                CC[i][0] = compStdCurves[18 * (col - 1) + i][0];
                CC[i][1] = compStdCurves[18 * (col - 1) + i][10];
                for (int j = 0; j < 10; j++)
                {
                    CC[i][j + 2] = compStdCurves[18 * (col - 1) + i][20 + j];
                }
            }

            double Tref = (35 + 273.15);
            double Pref = 0.972;
            double Tratio = Tin / Tref;
            double Pratio = Pin / Pref;

            double cX1 = -0.063276 * Math.Pow(Pratio, 2) + 1.139254 * Pratio - 0.075972;
            double cY1 = -0.071200 * Math.Pow(Pratio, 2) + 1.157932 * Pratio - 0.084129;
            double cX2 = -0.077294 * Math.Pow(Tratio, 2) + 1.072132 * Tratio + 0.005148;
            double cY2 = +0.138523 * Math.Pow(Tratio, 2) + 0.677078 * Tratio + 0.181374;

            double W = Func.Wcalculator(Pin, Tin, Hin);
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    CC[2 * i][j] *= (cX1 / cX2 * (1.0 + W) * (1.0 - 0.03 * (i / 10.0) * (Hin / 100.0)) * 0.985);
                    CC[2 * i + 1][j] *= (cY1 / cY2 * (1.0 - 0.02 * (Hin / 100.0)));
                }
            }

            double[] Uband = new double[9], Lband = new double[9];
            for (int i = 0; i < 9; i++)
            {
                double[] ptr1 = CC[2 * i];
                double[] ptr2 = CC[2 * i + 1];
                double[] SX = { ptr1[0], ptr1[1], ptr1[2] };
                double[] SY = { ptr2[0], ptr2[1], ptr2[2] };
                double[] bLfit = Func.Polyfit(SY, SX, 1);
                Lband[i] = Func.Polyeval(bLfit, Pd);
                double[] ix = new double[10];
                Array.ConstrainedCopy(ptr1, 2, ix, 0, 10);
                double[] iy = new double[10];
                Array.ConstrainedCopy(ptr2, 2, iy, 0, 10);
                double[] bUfit = Func.Polyfit(iy, ix, 4);
                Uband[i] = Func.Polyeval(bUfit, Pd);
            }

            int row = 0;
            if (Q <= Lband[0] || Q >= Uband[8])
            {
                selectedCompressor = "Flow Out of Range!!";
                return selectedCompressor;
            }
            else
            {
                for (int i = 0; i < 9; i++)
                {
                    if (Q < (Uband[i] * (1.08 - 0.0075 * i)))
                    {
                        row = i + 1;
                        break;
                    }
                }
            }

            double DTR = (Uband[row] - Lband[row]) / Uband[row];
            if (DTR < 0.22)
            {
                if (col > 1)
                {
                    col -= 1;
                }
            }
            if (DTR > 0.48)
            {
                if (col > 1)
                {
                    col += 1;
                }
            }

            for (int i = 0; i < 18; i++)
            {
                CC[i][0] = compStdCurves[18 * (col - 1) + i][0];
                CC[i][1] = compStdCurves[18 * (col - 1) + i][10];
                for (int j = 0; j < 10; j++)
                {
                    CC[i][j + 2] = compStdCurves[18 * (col - 1) + i][20 + j];
                }
            }

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    CC[2 * i][j] *= (cX1 / cX2 * (1.0 + W) * (1.0 - 0.03 * (row / 10.0) * (Hin / 100.0)) * 0.985);
                    CC[2 * i + 1][j] *= (cY1 / cY2 * (1.0 - 0.02 * (Hin / 100.0)));
                }
            }

            for (int i = 0; i < 9; i++)
            {
                double[] ptr1 = CC[2 * i];
                double[] ptr2 = CC[2 * i + 1];
                double[] SX = { ptr1[0], ptr1[1], ptr1[2] };
                double[] SY = { ptr2[0], ptr2[1], ptr2[2] };
                double[] bLfit = Func.Polyfit(SY, SX, 1);
                Lband[i] = Func.Polyeval(bLfit, Pd);
                double[] ix = new double[10];
                Array.ConstrainedCopy(ptr1, 2, ix, 0, 10);
                double[] iy = new double[10];
                Array.ConstrainedCopy(ptr2, 2, iy, 0, 10);
                double[] bUfit = Func.Polyfit(iy, ix, 4);
                Uband[i] = Func.Polyeval(bUfit, Pd);
            }

            if (Q > Lband[0] || Q < Uband[8])
            {
                for (int i = 0; i < 9; i++)
                {
                    if (Q < Uband[i])
                    {
                        row = i + 1;
                        break;
                    }
                }
            }

            int s = 4 * (row - 1) + (col - 1);
            selectedCompressor = listofCompressors[s];

            return selectedCompressor;
        }
        public CompressorReportType Analysis(InputDataType inCondition)
        {
            CompressorReportType report = new CompressorReportType
            {
                modelName = inCondition.modelName
            };

            double Pin = Func.UnitConvertor(inCondition.Pin_u, "bara", inCondition.Pin);
            double dPin = Func.UnitConvertor(inCondition.dPin_u, "bara", inCondition.dPin);
            Pin -= dPin;

            report.Pinlet = Pin;

            double Tin = Func.UnitConvertor(inCondition.Tin_u, "K", inCondition.Tin);
            double Hin = inCondition.Hin;
            double Q = Func.FlowCalculator(inCondition.Q_u, "kg/h", inCondition.Q, Pin + dPin, Tin, Hin) / 3600.0;
            double Pd = Func.UnitConvertor(inCondition.Pd_u, "bara", inCondition.Pd);

            int row = 0, col = 0;
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int s = 4 * i + j;
                    if (inCondition.modelName == listofCompressors[s])
                    {
                        col = j + 1;
                        row = i + 1;
                    }
                }
            }

            double[] MC = new double[30];
            double[] PC = new double[30];
            double[] KC = new double[30];

            for (int i = 0; i < 30; i++)
            {
                MC[i] = compStdCurves[18 * (col - 1) + 2 * (row - 1)][i];
                PC[i] = compStdCurves[18 * (col - 1) + 2 * (row - 1) + 1][i];
                KC[i] = compPwrCurves[18 * (col - 1) + 2 * (row - 1) + 1][i];
            }

            double Tref = (35.0 + 273.15);
            double Pref = 0.972;
            double Tratio = Tin / Tref;
            double Pratio = Pin / Pref;

            double cX1 = -0.063276 * Math.Pow(Pratio, 2) + 1.139254 * Pratio - 0.075972;
            double cY1 = -0.071200 * Math.Pow(Pratio, 2) + 1.157932 * Pratio - 0.084129;
            double cZ1 = -0.038690 * Math.Pow(Pratio, 2) + 0.946919 * Pratio + 0.092064;
            double cX2 = -0.077294 * Math.Pow(Tratio, 2) + 1.072132 * Tratio + 0.005148;
            double cY2 = +0.138523 * Math.Pow(Tratio, 2) + 0.677078 * Tratio + 0.181374;
            double cZ2 = -0.000804 * Math.Pow(Tratio, 2) + 0.832824 * Tratio + 0.167411;

            double W = Func.Wcalculator(Pin, Tin, Hin);

            for (int i = 0; i < 30; i++)
            {
                MC[i] *= (cX1 / cX2 * (1.0 + W) * (1.0 - 0.03 * (row / 10.0) * (Hin / 100.0)) * 0.985);
                PC[i] *= (cY1 / cY2 * (1.0 - 0.02 * (Hin / 100.0)));
                KC[i] *= (cZ1 / cZ2);
            }

            double xlim1 = ((MC[0] - 0.1 * MC[29]) < (0.9 * Q) ? (MC[0] - 0.1 * MC[29]) : (0.9 * Q));
            double xlim2 = ((MC[29] + 0.1 * MC[29]) < (1.1 * Q) ? (1.1 * Q) : (MC[29] + 0.1 * MC[29]));
            double ylim1 = ((PC[9] - 0.15 * PC[20]) < (0.85 * Pd) ? (PC[9] - 0.15 * PC[20]) : (0.85 * Pd));
            double ylim2 = ((PC[20] + 0.15 * PC[20]) < (1.15 * Pd) ? (1.15 * Pd) : (PC[20] + 0.15 * PC[20]));
            double zlim1 = KC[0] - 0.1 * KC[0];
            double zlim2 = KC[29] + 0.1 * KC[29];

            double[] SX = { MC[0], MC[10], MC[20] };
            double[] SY = { PC[0], PC[10], PC[20] };
            double[] PY = { KC[0], KC[10], KC[20] };
            double[] bLfit = Func.Polyfit(SY, SX, 1);
            double[] bKfit = Func.Polyfit(SX, PY, 1);
            double Lband;
            if (Pd <= PC[20] && Pd >= PC[9])
            {
                Lband = Func.Polyeval(bLfit, Pd);
            }
            else
            {
                Lband = 0;
            }

            double[] ix1 = new double[10];
            Array.ConstrainedCopy(MC, 20, ix1, 0, 10);
            double[] iy1 = new double[10];
            Array.ConstrainedCopy(PC, 20, iy1, 0, 10);
            double[] iz1 = new double[10];
            Array.ConstrainedCopy(KC, 20, iz1, 0, 10);

            double[] bUfit1 = Func.Polyfit(iy1, ix1, 4);
            double[] bKfit1 = Func.Polyfit(ix1, iz1, 4);
            double Uband;
            if (Pd <= PC[20] && Pd >= PC[19])
            {
                Uband = Func.Polyeval(bUfit1, Pd);
            }
            else
            {
                Uband = 0;
            }

            double[] ix2 = new double[10];
            Array.ConstrainedCopy(MC, 0, ix2, 0, 10);
            double[] iy2 = new double[10];
            Array.ConstrainedCopy(PC, 0, iy2, 0, 10);
            double[] iz2 = new double[10];
            Array.ConstrainedCopy(KC, 0, iz2, 0, 10);

            double[] bUfit2 = Func.Polyfit(iy2, ix2, 4);
            double[] bKfit2 = Func.Polyfit(ix2, iz2, 4);
            double band30;
            if (Pd <= PC[0] && Pd >= PC[9])
            {
                band30 = Func.Polyeval(bUfit2, Pd);
            }
            else
            {
                band30 = 0;
            }

            double[] ix3 = new double[10];
            Array.ConstrainedCopy(MC, 10, ix3, 0, 10);
            double[] iy3 = new double[10];
            Array.ConstrainedCopy(PC, 10, iy3, 0, 10);
            double[] iz3 = new double[10];
            Array.ConstrainedCopy(KC, 10, iz3, 0, 10);

            double[] bUfit3 = Func.Polyfit(iy3, ix3, 4);
            double[] bKfit3 = Func.Polyfit(ix3, iz3, 4);
            double band70;
            if (Pd <= PC[10] && Pd >= PC[19])
            {
                band70 = Func.Polyeval(bUfit3, Pd);
            }
            else
            {
                band70 = 0;
            }

            double LK, UK, K30, K70;
            if (Lband != 0.0)
            {
                LK = Func.Polyeval(bKfit, Lband);
            }
            else
            {
                LK = 0.0;
            }
            if (Uband != 0.0)
            {
                UK = Func.Polyeval(bKfit1, Uband);
            }
            else
            {
                UK = 0.0;
            }
            if (band30 != 0.0)
            {
                K30 = Func.Polyeval(bKfit2, band30);
            }
            else
            {
                K30 = 0.0;
            }
            if (band70 != 0.0)
            {
                K70 = Func.Polyeval(bKfit3, band70);
            }
            else
            {
                K70 = 0.0;
            }

            double[] SL = Func.Polyfit(SX, SY, 1);
            double S1 = Func.Polyeval(SL, xlim1);
            double S2 = Func.Polyeval(SL, xlim2);

            double power = UK + ((Q - Uband) / (Lband - Uband)) * (LK - UK);

            double qexchange = 0;
            int CSN = 0;
            double dpower = 0;
            double dpic1 = 0, dpic2 = 0, dpac = 0, dpoc = 0;
            if (report.modelName == "HYTC400-1" || report.modelName == "HYTC400-2" || report.modelName == "HYTC400-3" || report.modelName == "HYTC400-4")
            {
                qexchange = 329;
                CSN = 1;
                dpower = 400.0 / 1.341;
                dpic1 = 0.23;
                dpic2 = 0.19;
                dpac = 0.22;
                dpoc = 0.12;
                report.OilPumpKW = 0.75;
                report.OilHeater = 4;
                report.Sst1 = 36000;
                report.Sst2 = 36000;
                report.Sst3 = 48000;
                report.Dinlet = 8;
                report.Ddischarge = 4;
                report.Dblowoff = 2;
                report.Doutlet = 12;
            }
            else if (report.modelName == "HYTC500-1" || report.modelName == "HYTC500-2" || report.modelName == "HYTC500-3" || report.modelName == "HYTC500-4")
            {
                qexchange = 380;
                CSN = 2;
                dpower = 500.0 / 1.341;
                dpic1 = 0.38;
                dpic2 = 0.32;
                dpac = 0.37;
                dpoc = 0.12;
                report.OilPumpKW = 0.75;
                report.OilHeater = 4;
                report.Sst1 = 36000;
                report.Sst2 = 36000;
                report.Sst3 = 48000;
                report.Dinlet = 8;
                report.Ddischarge = 4;
                report.Dblowoff = 2;
                report.Doutlet = 12;
            }
            else if (report.modelName == "HYTC600-1" || report.modelName == "HYTC600-2" || report.modelName == "HYTC600-3" || report.modelName == "HYTC600-4")
            {
                qexchange = 432;
                CSN = 3;
                dpower = 600.0 / 1.341;
                dpic1 = 0.58;
                dpic2 = 0.49;
                dpac = 0.56;
                dpoc = 0.12;
                report.OilPumpKW = 0.75;
                report.OilHeater = 4;
                report.Sst1 = 36000;
                report.Sst2 = 36000;
                report.Sst3 = 48000;
                report.Dinlet = 8;
                report.Ddischarge = 4;
                report.Dblowoff = 2;
                report.Doutlet = 12;
            }
            else if (report.modelName == "HYTB700-1" || report.modelName == "HYTB700-2" || report.modelName == "HYTB700-3" || report.modelName == "HYTB700-4")
            {
                qexchange = 506;
                CSN = 4;
                dpower = 700.0 / 1.341;
                dpic1 = 0.07;
                dpic2 = 0.07;
                dpac = 0.10;
                dpoc = 0.13;
                report.OilPumpKW = 1.5;
                report.OilHeater = 4;
                report.Sst1 = 29790;
                report.Sst2 = 29790;
                report.Sst3 = 38939;
                report.Dinlet = 10;
                report.Ddischarge = 5;
                report.Dblowoff = 4;
                report.Doutlet = 12;
            }
            else if (report.modelName == "HYTB800-1" || report.modelName == "HYTB800-2" || report.modelName == "HYTB800-3" || report.modelName == "HYTB800-4")
            {
                qexchange = 581;
                CSN = 5;
                dpower = 800.0 / 1.341;
                dpic1 = 0.12;
                dpic2 = 0.12;
                dpac = 0.19;
                dpoc = 0.13;
                report.OilPumpKW = 1.5;
                report.OilHeater = 4;
                report.Sst1 = 29790;
                report.Sst2 = 29790;
                report.Sst3 = 38939;
                report.Dinlet = 10;
                report.Ddischarge = 5;
                report.Dblowoff = 4;
                report.Doutlet = 12;
            }
            else if (report.modelName == "HYTB900-1" || report.modelName == "HYTB900-2" || report.modelName == "HYTB900-3" || report.modelName == "HYTB900-4")
            {
                qexchange = 657;
                CSN = 6;
                dpower = 900.0 / 1.341;
                dpic1 = 0.17;
                dpic2 = 0.17;
                dpac = 0.22;
                dpoc = 0.13;
                report.OilPumpKW = 1.5;
                report.OilHeater = 4;
                report.Sst1 = 29790;
                report.Sst2 = 29790;
                report.Sst3 = 38939;
                report.Dinlet = 10;
                report.Ddischarge = 5;
                report.Dblowoff = 4;
                report.Doutlet = 12;
            }
            else if (report.modelName == "HYTA1000-1" || report.modelName == "HYTA1000-2" || report.modelName == "HYTA1000-3" || report.modelName == "HYTA1000-4")
            {
                qexchange = 750;
                CSN = 7;
                dpower = 1000.0 / 1.341;
                dpic1 = 0.16;
                dpic2 = 0.16;
                dpac = 0.19;
                dpoc = 0.17;
                report.OilPumpKW = 1.5;
                report.OilHeater = 4;
                report.Sst1 = 22000;
                report.Sst2 = 28000;
                report.Sst3 = 28000;
                report.Dinlet = 12;
                report.Ddischarge = 6;
                report.Dblowoff = 4;
                report.Doutlet = 12;
            }
            else if (report.modelName == "HYTA1250-1" || report.modelName == "HYTA1250-2" || report.modelName == "HYTA1250-3" || report.modelName == "HYTA1250-4")
            {
                qexchange = 910;
                CSN = 8;
                dpower = 1250.0 / 1.341;
                dpic1 = 0.27;
                dpic2 = 0.27;
                dpac = 0.19;
                dpoc = 0.15;
                report.OilPumpKW = 1.5;
                report.OilHeater = 4;
                report.Sst1 = 22000;
                report.Sst2 = 28000;
                report.Sst3 = 28000;
                report.Dinlet = 12;
                report.Ddischarge = 6;
                report.Dblowoff = 4;
                report.Doutlet = 12;
            }
            else if (report.modelName == "HYTA1500-1" || report.modelName == "HYTA1500-2" || report.modelName == "HYTA1500-3" || report.modelName == "HYTA1500-4")
            {
                qexchange = 1019;
                CSN = 9;
                dpower = 1500.0 / 1.341;
                dpic1 = 0.49;
                dpic2 = 0.49;
                dpac = 0.23;
                dpoc = 0.27;
                report.OilPumpKW = 1.5;
                report.OilHeater = 4;
                report.Sst1 = 22000;
                report.Sst2 = 28000;
                report.Sst3 = 28000;
                report.Dinlet = 12;
                report.Ddischarge = 6;
                report.Dblowoff = 4;
                report.Doutlet = 12;
            };

            double TCin = inCondition.Tcw_in;
            double TriseIn = inCondition.Tcw_rise;
            double[] Xin = new double[20];
            double[] Yin1 = new double[20];
            double[] Yin2 = new double[20];
            double[] Yin3 = new double[20];
            double[] Yin4 = new double[20];
            Array.ConstrainedCopy(cwrequirement[0], 20 * (CSN - 1), Xin, 0, 20);
            Array.ConstrainedCopy(cwrequirement[1], 20 * (CSN - 1), Yin1, 0, 20);
            Array.ConstrainedCopy(cwrequirement[2], 20 * (CSN - 1), Yin2, 0, 20);
            Array.ConstrainedCopy(cwrequirement[3], 20 * (CSN - 1), Yin3, 0, 20);
            Array.ConstrainedCopy(cwrequirement[4], 20 * (CSN - 1), Yin4, 0, 20);
            double mIC1 = Func.PlinearInterpolation(Xin, Yin1, TCin);
            double mIC2 = Func.PlinearInterpolation(Xin, Yin2, TCin);
            double mAC = Func.PlinearInterpolation(Xin, Yin3, TCin);
            double mOC = Func.PlinearInterpolation(Xin, Yin4, TCin);

            double Trise = qexchange / ((mIC1 + mIC2 + mAC + mOC) * 4.178);
            if (Trise > 10)
            {
                mIC1 *= (Trise / TriseIn);
                mIC2 *= (Trise / TriseIn);
                mAC *= (Trise / TriseIn);
                mOC *= (Trise / TriseIn);
            }
            Trise = qexchange / ((mIC1 + mIC2 + mAC + mOC) * 4.178);

            double wfactor = UK / dpower;

            mIC1 *= wfactor;
            mIC2 *= wfactor;
            mAC *= wfactor;
            mOC *= wfactor;

            dpic1 *= Math.Pow((mIC1 / Yin1[16]), 2);
            dpic2 *= Math.Pow((mIC2 / Yin2[16]), 2);
            dpac *= Math.Pow((mAC / Yin3[16]), 2);
            dpoc *= Math.Pow((mOC / Yin4[16]), 2);

            mIC1 *= (3600.0 / 997.0);
            mIC2 *= (3600.0 / 997.0);
            mAC *= (3600.0 / 997.0);
            mOC *= (3600.0 / 997.0);

            double mtotal = mIC1 + mIC2 + mAC + mOC;

            double ax = 0, bx = 0;
            if (report.modelName == "HYTC400-1")
            {
                ax = 4.89480;
                bx = 0.03070;
            }
            else if (report.modelName == "HYTC400-2")
            {
                ax = 5.00680;
                bx = 0.04320;
            }
            else if (report.modelName == "HYTC400-3")
            {
                ax = 5.35240;
                bx = 0.01800;
            }
            else if (report.modelName == "HYTC400-4")
            {
                ax = 5.62080;
                bx = 0.03990;
            }
            else if (report.modelName == "HYTC500-1")
            {
                ax = 4.88770;
                bx = 0.06370;
            }
            else if (report.modelName == "HYTC500-2")
            {
                ax = 4.98930;
                bx = 0.05030;
            }
            else if (report.modelName == "HYTC500-3")
            {
                ax = 5.32950;
                bx = 0.05480;
            }
            else if (report.modelName == "HYTC500-4")
            {
                ax = 5.60380;
                bx = -0.00007;
            }
            else if (report.modelName == "HYTC600-1")
            {
                ax = 4.955110;
                bx = 0.09120;
            }
            else if (report.modelName == "HYTC600-2")
            {
                ax = 5.10200;
                bx = 0.09680;
            }
            else if (report.modelName == "HYTC600-3")
            {
                ax = 5.40440;
                bx = 0.09750;
            }
            else if (report.modelName == "HYTB600-4")
            {
                ax = 5.51940;
                bx = 0.09330;
            }
            else if (report.modelName == "HYTB700-1")
            {
                ax = 4.80530;
                bx = 0.14930;
            }
            else if (report.modelName == "HYTB700-2")
            {
                ax = 5.14820;
                bx = 0.15360;
            }
            else if (report.modelName == "HYTB700-3")
            {
                ax = 5.30680;
                bx = 0.16350;
            }
            else if (report.modelName == "HYTB700-4")
            {
                ax = 5.57800;
                bx = 0.12580;
            }
            else if (report.modelName == "HYTB800-1")
            {
                ax = 5.03240;
                bx = -0.10840;
            }
            else if (report.modelName == "HYTB800-2")
            {
                ax = 5.33950;
                bx = 0.03600;
            }
            else if (report.modelName == "HYTB800-3")
            {
                ax = 5.86100;
                bx = 0.25960;
            }
            else if (report.modelName == "HYTB800-4")
            {
                ax = 6.05580;
                bx = 0.24150;
            }
            else if (report.modelName == "HYTB900-1")
            {
                ax = 4.89840;
                bx = 0.14470;
            }
            else if (report.modelName == "HYTB900-2")
            {
                ax = 5.09160;
                bx = 0.15670;
            }
            else if (report.modelName == "HYTB900-3")
            {
                ax = 5.36500;
                bx = 0.20680;
            }
            else if (report.modelName == "HYTB900-4")
            {
                ax = 5.52170;
                bx = 0.12270;
            }
            else if (report.modelName == "HYTA1000-1")
            {
                ax = 4.86180;
                bx = 0.15300;
            }
            else if (report.modelName == "HYTA1000-2")
            {
                ax = 5.02390;
                bx = 0.19940;
            }
            else if (report.modelName == "HYTA1000-3")
            {
                ax = 5.41030;
                bx = 0.15750;
            }
            else if (report.modelName == "HYTA1000-4")
            {
                ax = 5.50550;
                bx = 0.24130;
            }
            else if (report.modelName == "HYTA1250-1")
            {
                ax = 4.82480;
                bx = 0.19240;
            }
            else if (report.modelName == "HYTA1250-2")
            {
                ax = 5.04280;
                bx = 0.20920;
            }
            else if (report.modelName == "HYTA1250-3")
            {
                ax = 5.31100;
                bx = 0.27900;
            }
            else if (report.modelName == "HYTA1250-4")
            {
                ax = 5.48610;
                bx = 0.34610;
            }
            else if (report.modelName == "HYTA1500-1")
            {
                ax = 4.97080;
                bx = 0.15430;
            }
            else if (report.modelName == "HYTA1500-2")
            {
                ax = 4.88920;
                bx = 0.29990;
            }
            else if (report.modelName == "HYTA1500-3")
            {
                ax = 5.25750;
                bx = 0.28060;
            }
            else if (report.modelName == "HYTA1500-4")
            {
                ax = 5.37160;
                bx = 0.35710;
            }

            double Rind = (Pin * 100000.0) / (0.287 * 1000.0 * Tin);
            double RoutS = (ax * (Rind * (Tin / (35 + 273.15))) + bx) * (0.1350 * (Pd / Pin) + 0.0945);
            double Tout = ((Pd * 100000.0) / (RoutS * 0.287 * 1000.0)) - 273.15;

            Tout *= (1 + 0.1 * W);

            Q *= 3600.0;
            Q = Func.FlowCalculator("kg/h", inCondition.Q_u, Q, Pin + dPin, Tin, Hin);
            Pd = Func.UnitConvertor("bara", inCondition.Pd_u, inCondition.Pd);

            double CX;

            xlim1 *= 3600.0;
            xlim1 = Func.FlowCalculator("kg/h", inCondition.Q_u, xlim1, Pin + dPin, Tin, Hin);
            CX = Math.Pow(10, (Convert.ToInt32(Math.Ceiling(Math.Log(xlim1) / Math.Log(10.0))) - 2));
            xlim1 = Math.Floor(xlim1 / CX) * CX;
            report.p_Border[0] = xlim1;
            report.k_Border[0] = xlim1;

            xlim2 *= 3600.0;
            xlim2 = Func.FlowCalculator("kg/h", inCondition.Q_u, xlim2, Pin + dPin, Tin, Hin);
            CX = Math.Pow(10, (Convert.ToInt32(Math.Ceiling(Math.Log(xlim2) / Math.Log(10.0))) - 2));
            xlim2 = Math.Floor(xlim2 / CX) * CX;
            report.p_Border[1] = xlim2;
            report.k_Border[1] = xlim2;

            ylim1 = Func.UnitConvertor("bara", inCondition.Pd_u, ylim1);
            CX = Math.Pow(10, (Convert.ToInt32(Math.Ceiling(Math.Log(ylim1) / Math.Log(10.0))) - 2));
            ylim1 = Math.Floor(ylim1 / CX) * CX;
            report.p_Border[2] = ylim1;

            ylim2 = Func.UnitConvertor("bara", inCondition.Pd_u, ylim2);
            CX = Math.Pow(10, (Convert.ToInt32(Math.Ceiling(Math.Log(ylim2) / Math.Log(10.0))) - 2));
            ylim2 = Math.Floor(ylim2 / CX) * CX;
            report.p_Border[3] = ylim2;

            CX = Math.Pow(10, (Convert.ToInt32(Math.Ceiling(Math.Log(zlim1) / Math.Log(10.0))) - 2));
            zlim1 = Math.Floor(zlim1 / CX) * CX;
            report.k_Border[2] = zlim1;

            CX = Math.Pow(10, (Convert.ToInt32(Math.Ceiling(Math.Log(zlim2) / Math.Log(10.0))) - 2));
            zlim2 = Math.Floor(zlim2 / CX) * CX;
            report.k_Border[3] = zlim2;

            report.p_DP[0] = Q;
            report.p_DP[1] = Pd;

            Lband *= 3600.0;
            Lband = Func.FlowCalculator("kg/h", inCondition.Q_u, Lband, Pin + dPin, Tin, Hin);

            Uband *= 3600.0;
            Uband = Func.FlowCalculator("kg/h", inCondition.Q_u, Uband, Pin + dPin, Tin, Hin);

            report.MaxF = Uband;
            report.MaxP = UK;
            report.CPF = power;
            report.DTR = Math.Round(((Uband - Lband) / Uband) * 1000.0) / 10.0;
            report.RTR = Math.Round(((Q - Lband) / Uband) * 1000.0) / 10.0;

            report.p_TurnDown[0] = Lband;
            report.p_TurnDown[1] = Uband;

            report.p_TurnDown[2] = Pd;
            report.p_TurnDown[3] = Pd;
            report.p_SurgeLine[0] = xlim1;
            report.p_SurgeLine[1] = xlim2;

            S1 = Func.UnitConvertor("bara", inCondition.Pd_u, S1);
            report.p_SurgeLine[2] = S1;

            S2 = Func.UnitConvertor("bara", inCondition.Pd_u, S2);
            report.p_SurgeLine[3] = S2;

            report.mic = mIC1 + mIC2;
            report.mac = mAC;
            report.moc = mOC;
            report.mtot = mtotal;
            report.wTrise = Trise;

            report.Te3 = Tout;

            report.dp_mic = Math.Max(dpic1, dpic2);
            report.dp_mac = dpac;
            report.dp_moc = dpoc;

            if (Q < Uband)
            {
                report.k_DP[0] = Q;
                report.k_DP[1] = power;
            }
            else
            {
                report.k_DP[0] = Q;
                report.k_DP[1] = 0.0;
            }

            band30 *= 3600.0;
            band30 = Func.FlowCalculator("kg/h", inCondition.Q_u, band30, Pin + dPin, Tin, Hin);

            band70 *= 3600.0;
            band70 = Func.FlowCalculator("kg/h", inCondition.Q_u, band70, Pin + dPin, Tin, Hin);

            report.k_TurnDown[1] = Uband;
            report.k_TurnDown[3] = UK;
            double A = Q;
            double B = power;
            if (band70 < A && band70 > 0.0)
            {
                A = band70;
                B = K70;
            }
            if (band30 < A && band30 > 0.0)
            {
                A = band30;
                B = K30;
            }

            report.k_TurnDown[0] = A;
            report.k_TurnDown[2] = B;

            for (int i = 0; i < 10; i++)
            {
                MC[i] *= 3600.0;
                MC[i] = Func.FlowCalculator("kg/h", inCondition.Q_u, MC[i], Pin, Tin, Hin);
                report.p_IGV30[i] = MC[i];

                report.k_IGV30[i] = MC[i];

                PC[i] = Func.UnitConvertor("bara", inCondition.Pd_u, PC[i]);
                report.p_IGV30[i + 10] = PC[i];

                report.k_IGV30[i + 10] = KC[i];

                MC[i + 10] *= 3600.0;
                MC[i + 10] = Func.FlowCalculator("kg/h", inCondition.Q_u, MC[i + 10], Pin, Tin, Hin);
                report.p_IGV70[i] = MC[i + 10];

                report.k_IGV70[i] = MC[i + 10];

                PC[i + 10] = Func.UnitConvertor("bara", inCondition.Pd_u, PC[i + 10]);
                report.p_IGV70[i + 10] = PC[i + 10];

                report.k_IGV70[i + 10] = KC[i + 10];

                MC[i + 20] *= 3600.0;
                MC[i + 20] = Func.FlowCalculator("kg/h", inCondition.Q_u, MC[i + 20], Pin, Tin, Hin);
                report.p_IGV100[i] = MC[i + 20];

                report.k_IGV100[i] = MC[i + 20];

                PC[i + 20] = Func.UnitConvertor("bara", inCondition.Pd_u, PC[i + 20]);
                report.p_IGV100[i + 10] = PC[i + 20];

                report.k_IGV100[i + 10] = KC[i + 20];

            }

            report.DeliveredFlow = Math.Round(Func.FlowCalculator(inCondition.Q_u, "Nm^3/h", inCondition.Q, Pin + dPin, Tin, Hin),0).ToString();
            report.InletCoolingWaterTemp = Math.Round(Func.FlowCalculator(inCondition.Q_u, "kg/h", inCondition.Q, Pin + dPin, Tin, Hin),0).ToString();
            report.Pressure = Math.Round(Func.UnitConvertor(inCondition.Pin_u, "bara", inCondition.Pin),3).ToString();
            report.Temprature = Math.Round(Func.UnitConvertor(inCondition.Tin_u, "C", inCondition.Tin),0).ToString();
            report.Pressure2 = Math.Round(Func.UnitConvertor(inCondition.Pd_u, "bara", inCondition.Pd),3).ToString();

            return report;

        }

        private static string ReadRequiredLine(StreamReader reader, string path)
        {
            var line = reader.ReadLine();
            if (line == null)
                throw new InvalidDataException("Unexpected end of file: " + path);
            return line;
        }

        private static double ParseNumber(string value)
        {
            return double.Parse(value.Trim(), CultureInfo.InvariantCulture);
        }
    }
}
