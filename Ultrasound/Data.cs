using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//using System.Drawing;
//using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.Drawing;
using System.Diagnostics;
//using System.Windows;
//using System.Numerics;
using Exocortex.DSP;
using System.Threading.Tasks;

namespace Ultrasound
{

    public enum SliceFormingType { ns, opv, opp, ost };
    public enum AmplifyType { linear, pow, log };
    static class Data
    {
        private static object lockObj = new object();
        public static string filename;
        public static string modelName;

        public static int width;
        public static int zoneWidth;
        public static int zoneHeight;
        public static int maxtime;
        public static int numberOfTraces;
        public static int numberOfSonograms;
        public static int step;//шаг, с которым расположены трансмиттеры
        public static float dx, dy;//в метрах
        public static double dt;
        //public static int difRadius;

        public static int diffWidth = 30;
        public static int envSpeed = 0;
        public static double envAbs = 0;
        public static int[,] speedMap;
        public static float[,] speedMapOst;
        public static int smWidth, smHeight;
        
        public static Trace[] tracesData;//набор трасс
        public static Trace[] tracesDataDeconv;//набор трасс после деконволюции

        public static float[][,] slices;

        public static bool useDeconvolvedData;
        public static double curDecA = 0.1;

        //public static float[] signal = { 0, 0.688605F, 0.0746371F, -0.320453F, -0.0703439F, 0.145268F, 0.0493075F, -0.0639651F, -0.0304584F, 0.0272173F, 0.0174822F, 0, 0, 0 };
        //public static float[] signal = { 0.0f, 0.049901336421413582f, 0.012533323356430454f, -0.12769734259472953f, -0.0323296853314312f, 0.12363734711836992f, 0.058899928429548734f, -0.14477232839456303f, -0.086715661338308936f, 0.16042230584538295f, 0.13519060802726859f, -0.19262831069394754f, -0.20536413177860582f, 0.61609239533582116f, 0.7319875806369972f, -0.58778525229247813f, -0.84432792550201252f, 0.48175367410171815f, 0.90482705246601725f, -0.33131209741621637f, -0.808398038850879f, 0.14921393229891752f, 0.49114362536434342f, -0.025066646712862559f, -0.29940801852848131f, 0.0000000000000025482654181230304f, 0.29940801852848165f, 0.037599970069288363f, -0.19645745014573834f, -0.024868988716484131f, 0.19021130325903149f, 0.036812455268466777f };
        //public static float[] signal = { 0, 0.01154333f, 0.02304818f, 0.03447618f, 0.04578925f, 0.05694965f, 0.06792018f, 0.07866427f, 0.08914609f, 0.09933069f, 0.1091841f, 0.1186735f, 0.1277673f, 0.1364351f, 0.144648f, 0.1523787f, 0.1596013f, 0.1662918f, 0.1724279f, 0.1779892f, 0.182957f, 0.1873148f, 0.1910482f, 0.1941445f, 0.1965936f, 0.1983873f, 0.1995195f, 0.1999866f, 0.1997869f, 0.1989211f, 0.1973921f, 0.195205f, 0.1923671f, 0.1888878f, 0.1847788f, 0.1800537f, 0.1747284f, 0.1688205f, 0.1623497f, 0.1553377f, 0.1478078f, 0.1397851f, 0.1312963f, 0.1223698f, 0.1130353f, 0.103324f, 0.09326819f, 0.08290142f, 0.07225826f, 0.06137419f, 0.0502855f, 0.03902915f, 0.02764269f, 0.01616407f, 0.00463155f, -0.007105037f, -0.0194472f, -0.03235147f, -0.04577045f, -0.05965291f, -0.07394405f, -0.08858567f, -0.1035164f, -0.118672f, -0.1339855f, -0.1493877f, -0.1648072f, -0.1801708f, -0.1954039f, -0.2104307f, -0.2251749f, -0.2395592f, -0.2535067f, -0.2669406f, -0.2797845f, -0.2919632f, -0.3034028f, -0.314031f, -0.3237776f, -0.3325747f, -0.3403572f, -0.3470632f, -0.3526339f, -0.3570144f, -0.3601539f, -0.3620057f, -0.3625278f, -0.361683f, -0.3594393f, -0.3557697f, -0.3506531f, -0.344074f, -0.3360224f, -0.3264948f, -0.3154936f, -0.3030272f, -0.2891104f, -0.2737646f, -0.257017f, -0.2389015f, -0.2194581f, -0.1987329f, -0.1767784f, -0.1536529f, -0.1294204f, -0.1041509f, -0.07791957f, -0.05080708f, -0.02289898f, 0.005714433f, 0.03520138f, 0.06563723f, 0.09691449f, 0.1289193f, 0.1615318f, 0.1946269f, 0.2280741f, 0.2617388f, 0.2954821f, 0.3291618f, 0.3626329f, 0.395748f, 0.428358f, 0.4603131f, 0.4914628f, 0.5216569f, 0.550746f, 0.5785827f, 0.6050213f, 0.6299194f, 0.6531378f, 0.6745418f, 0.6940014f, 0.711392f, 0.7265953f, 0.7394994f, 0.75f, 0.7580002f, 0.7634116f, 0.7661549f, 0.7661598f, 0.7633657f, 0.7577223f, 0.7491899f, 0.7377395f, 0.7233532f, 0.7060245f, 0.6857586f, 0.6625723f, 0.6364943f, 0.6075652f, 0.5758376f, 0.541376f, 0.5042565f, 0.4645673f, 0.4224078f, 0.3778889f, 0.3311324f, 0.282271f, 0.2314475f, 0.1788149f, 0.1245355f, 0.06878076f, 0.01173044f, -0.04575089f, -0.1024507f, -0.1581827f, -0.2127659f, -0.2660252f, -0.3177919f, -0.3679044f, -0.4162085f, -0.4625582f, -0.5068156f, -0.5488517f, -0.5885467f, -0.6257905f, -0.6604825f, -0.6925323f, -0.7218598f, -0.7483954f, -0.7720798f, -0.7928648f, -0.8107126f, -0.8255965f, -0.8375003f, -0.8464185f, -0.8523564f, -0.8553296f, -0.855364f, -0.8524956f, -0.8467703f, -0.8382437f, -0.8269802f, -0.8130537f, -0.7965463f, -0.7775484f, -0.7561581f, -0.7324808f, -0.7066289f, -0.678721f, -0.6488816f, -0.6172405f, -0.5839324f, -0.5490962f, -0.5128743f, -0.4754126f, -0.4368594f, -0.3973649f, -0.3570809f, -0.3161598f, -0.2747546f, -0.2330177f, -0.191101f, -0.1491545f, -0.1073269f, -0.06576395f, -0.02460869f, 0.01599937f, 0.05551623f, 0.09363686f, 0.1302418f, 0.1652208f, 0.1984727f, 0.2299064f, 0.2594404f, 0.2870035f, 0.3125347f, 0.3359834f, 0.3573093f, 0.3764825f, 0.3934838f, 0.4083039f, 0.4209439f, 0.4314148f, 0.4397374f, 0.4459422f, 0.4500687f, 0.4521653f, 0.4522891f, 0.4505053f, 0.4468866f, 0.4415132f, 0.4344718f, 0.4258555f, 0.415763f, 0.4042981f, 0.391569f, 0.377688f, 0.3627708f, 0.3469356f, 0.3303029f, 0.3129944f, 0.2951329f, 0.2768413f, 0.258242f };
        public static float[] signal = { 0.0000000e+00f, 2.4705567e-05f, 2.0080991e-04f, 6.7034760e-04f, 1.5285072e-03f, 2.7887512e-03f, 4.3621813e-03f, 6.0582370e-03f, 7.6099872e-03f, 8.9451009e-03f, 1.0357220e-02f, 1.1854203e-02f, 1.3434635e-02f, 1.5096378e-02f, 1.6836528e-02f, 1.8651363e-02f, 2.0536290e-02f, 2.2485809e-02f, 2.4493465e-02f, 2.6551804e-02f, 2.8652346e-02f, 3.0785553e-02f, 3.2940805e-02f, 3.5106368e-02f, 3.7269410e-02f, 3.9415959e-02f, 4.1530937e-02f, 4.3598153e-02f, 4.5600336e-02f, 4.7519144e-02f, 4.9335226e-02f, 5.1028263e-02f, 5.2577026e-02f, 5.3959444e-02f, 5.5152707e-02f, 5.6133337e-02f, 5.6877315e-02f, 5.7360191e-02f, 5.7557207e-02f, 5.7443462e-02f, 5.6994051e-02f, 5.6184221e-02f, 5.4989576e-02f, 5.3386237e-02f, 5.1351044e-02f, 4.8861768e-02f, 4.5897320e-02f, 4.2437952e-02f, 3.8465515e-02f, 3.3963643e-02f, 2.8918024e-02f, 2.3316601e-02f, 1.7149819e-02f, 1.0410842e-02f, 3.0957828e-03f, -4.7960808e-03f, -1.3262098e-02f, -2.2296047e-02f, -3.1887945e-02f, -4.2023882e-02f, -5.2685857e-02f, -6.3851640e-02f, -7.5494669e-02f, -8.7583944e-02f, -1.0008395e-01f, -1.1295464e-01f, -1.2615137e-01f, -1.3962500e-01f, -1.5332183e-01f, -1.6718377e-01f, -1.8114841e-01f, -1.9514917e-01f, -2.0911551e-01f, -2.2297308e-01f, -2.3664409e-01f, -2.5004750e-01f, -2.6309937e-01f, -2.7571326e-01f, -2.8780061e-01f, -2.9927114e-01f, -3.1003338e-01f, -3.1999511e-01f, -3.2906386e-01f, -3.3714759e-01f, -3.4415504e-01f, -3.4999654e-01f, -3.5458449e-01f, -3.5783401e-01f, -3.5966358e-01f, -3.5999569e-01f, -3.5875738e-01f, -3.5588104e-01f, -3.5130486e-01f, -3.4497359e-01f, -3.3683902e-01f, -3.2686061e-01f, -3.1500605e-01f, -3.0125168e-01f, -2.8558314e-01f, -2.6799560e-01f, -2.4849430e-01f, -2.2709483e-01f, -2.0382345e-01f, -1.7871724e-01f, -1.5182438e-01f, -1.2320417e-01f, -9.2927061e-02f, -6.1074678e-02f, -2.7739663e-02f, 6.9744838e-03f, 4.2953670e-02f, 8.0073528e-02f, 1.1819984e-01f, 1.5718900e-01f, 1.9688855e-01f, 2.3713785e-01f, 2.7776876e-01f, 3.1860632e-01f, 3.5946965e-01f, 4.0017286e-01f, 4.4052586e-01f, 4.8033547e-01f, 5.1940638e-01f, 5.5754209e-01f, 5.9454638e-01f, 6.3022399e-01f, 6.6438192e-01f, 6.9683081e-01f, 7.2738564e-01f, 7.5586730e-01f, 7.8210342e-01f, 8.0592954e-01f, 8.2719040e-01f, 8.4574068e-01f, 8.6144620e-01f, 8.7418485e-01f, 8.8384730e-01f, 8.9033806e-01f, 8.9357620e-01f, 8.9349574e-01f, 8.9004660e-01f, 8.8319486e-01f, 8.7292337e-01f, 8.5923189e-01f, 8.4213722e-01f, 8.2167357e-01f, 7.9789233e-01f, 7.7086210e-01f, 7.4066848e-01f, 7.0741349e-01f, 6.7121553e-01f, 6.3220865e-01f, 5.9054178e-01f, 5.4637843e-01f, 4.9989539e-01f, 4.5128208e-01f, 4.0073961e-01f, 3.4847954e-01f, 2.9472288e-01f, 2.3969890e-01f, 1.8364376e-01f, 1.2679933e-01f, 6.9411822e-02f, 1.1730438e-02f, -4.5994017e-02f, -1.0351054e-01f, -1.6056928e-01f, -2.1692297e-01f, -2.7232826e-01f, -3.2654709e-01f, -3.7934792e-01f, -4.3050709e-01f, -4.7981000e-01f, -5.2705216e-01f, -5.7204050e-01f, -6.1459404e-01f, -6.5454525e-01f, -6.9174051e-01f, -7.2604096e-01f, -7.5732338e-01f, -7.8548044e-01f, -8.1042135e-01f, -8.3207208e-01f, -8.5037583e-01f, -8.6529285e-01f, -8.7680072e-01f, -8.8489413e-01f, -8.8958496e-01f, -8.9090151e-01f, -8.8888872e-01f, -8.8360721e-01f, -8.7513298e-01f, -8.6355674e-01f, -8.4898311e-01f, -8.3152980e-01f, -8.1132692e-01f, -7.8851581e-01f, -7.6324826e-01f, -7.3568535e-01f, -7.0599639e-01f, -6.7435795e-01f, -6.4095265e-01f, -6.0596794e-01f, -5.6959516e-01f, -5.3202814e-01f, -4.9346226e-01f, -4.5409340e-01f, -4.1411650e-01f, -3.7372491e-01f, -3.3310896e-01f, -2.9245535e-01f, -2.5194588e-01f, -2.1175675e-01f, -1.7205767e-01f, -1.3301112e-01f, -9.4771549e-02f, -5.7484843e-02f, -2.1287683e-02f, 1.3692919e-02f, 4.7340058e-02f, 7.9547286e-02f, 1.1021888e-01f, 1.3927005e-01f, 1.6662708e-01f, 1.9222736e-01f, 2.1601939e-01f, 2.3796269e-01f, 2.5802764e-01f, 2.7619535e-01f, 2.9245722e-01f, 3.0681485e-01f, 3.1927946e-01f, 3.2987154e-01f, 3.3862048e-01f, 3.4556398e-01f, 3.5074741e-01f, 3.5422349e-01f, 3.5605150e-01f, 3.5629687e-01f, 3.5503030e-01f, 3.5232738e-01f, 3.4826782e-01f, 3.4293488e-01f, 3.3641466e-01f, 3.2879567e-01f, 3.2016793e-01f, 3.1062266e-01f, 3.0025154e-01f, 2.8914624e-01f, 2.7739778e-01f, 2.6509622e-01f, 2.5232998e-01f, 2.3918559e-01f, 2.2574714e-01f, 2.1209598e-01f, 1.9831035e-01f, 1.8446516e-01f, 1.7063159e-01f, 1.5687700e-01f, 1.4326461e-01f, 1.2985344e-01f, 1.1669817e-01f, 1.0384899e-01f, 9.1351636e-02f, 7.9247288e-02f, 6.7572616e-02f, 5.6359809e-02f, 4.5636635e-02f, 3.5426527e-02f, 2.5748687e-02f, 1.6618228e-02f, 8.0463104e-03f, 4.0317649e-05f, -7.3959655e-03f, -1.4262161e-02f, -2.0561092e-02f, -2.6298566e-02f, -3.1483162e-02f, -3.6126003e-02f, -4.0240526e-02f, -4.3842256e-02f, -4.6948578e-02f, -4.9578514e-02f, -5.1752489e-02f, -5.3492129e-02f, -5.4820038e-02f, -5.5759601e-02f, -5.6334782e-02f, -5.6569938e-02f, -5.6489646e-02f, -5.6118533e-02f, -5.5481113e-02f, -5.4601658e-02f, -5.3504050e-02f, -5.2211665e-02f, -5.0747264e-02f, -4.9132895e-02f, -4.7389805e-02f, -4.5538362e-02f, -4.3598000e-02f, -4.1587163e-02f, -3.9523251e-02f, -3.7422612e-02f, -3.5300497e-02f, -3.3171058e-02f, -3.1047333e-02f, -2.8941268e-02f, -2.6863704e-02f, -2.4824409e-02f, -2.2832101e-02f, -2.0894464e-02f, -1.9018197e-02f, -1.7209040e-02f, -1.5471818e-02f, -1.3810488e-02f, -1.2228184e-02f, -1.0727265e-02f, -9.3093645e-03f, -7.9664271e-03f, -6.3887839e-03f, -4.6444070e-03f, -3.0082264e-03f, -1.6805470e-03f, -7.6002488e-04f, -2.4136060e-04f, -3.4723744e-05f, -0.0000000e+00f };
        //public static float[] signal = { 0.0000000e+00f, 6.9744838e-03f, 4.2953670e-02f, 8.0073528e-02f, 1.1819984e-01f, 1.5718900e-01f, 1.9688855e-01f, 2.3713785e-01f, 2.7776876e-01f, 3.1860632e-01f, 3.5946965e-01f, 4.0017286e-01f, 4.4052586e-01f, 4.8033547e-01f, 5.1940638e-01f, 5.5754209e-01f, 5.9454638e-01f, 6.3022399e-01f, 6.6438192e-01f, 6.9683081e-01f, 7.2738564e-01f, 7.5586730e-01f, 7.8210342e-01f, 8.0592954e-01f, 8.2719040e-01f, 8.4574068e-01f, 8.6144620e-01f, 8.7418485e-01f, 8.8384730e-01f, 8.9033806e-01f, 8.9357620e-01f, 8.9349574e-01f, 8.9004660e-01f, 8.8319486e-01f, 8.7292337e-01f, 8.5923189e-01f, 8.4213722e-01f, 8.2167357e-01f, 7.9789233e-01f, 7.7086210e-01f, 7.4066848e-01f, 7.0741349e-01f, 6.7121553e-01f, 6.3220865e-01f, 5.9054178e-01f, 5.4637843e-01f, 4.9989539e-01f, 4.5128208e-01f, 4.0073961e-01f, 3.4847954e-01f, 2.9472288e-01f, 2.3969890e-01f, 1.8364376e-01f, 1.2679933e-01f, 6.9411822e-02f, 1.1730438e-02f, -4.5994017e-02f, -1.0351054e-01f, -1.6056928e-01f, -2.1692297e-01f, -2.7232826e-01f, -3.2654709e-01f, -3.7934792e-01f, -4.3050709e-01f, -4.7981000e-01f, -5.2705216e-01f, -5.7204050e-01f, -6.1459404e-01f, -6.5454525e-01f, -6.9174051e-01f, -7.2604096e-01f, -7.5732338e-01f, -7.8548044e-01f, -8.1042135e-01f, -8.3207208e-01f, -8.5037583e-01f, -8.6529285e-01f, -8.7680072e-01f, -8.8489413e-01f, -8.8958496e-01f, -8.9090151e-01f, -8.8888872e-01f, -8.8360721e-01f, -8.7513298e-01f, -8.6355674e-01f, -8.4898311e-01f, -8.3152980e-01f, -8.1132692e-01f, -7.8851581e-01f, -7.6324826e-01f, -7.3568535e-01f, -7.0599639e-01f, -6.7435795e-01f, -6.4095265e-01f, -6.0596794e-01f, -5.6959516e-01f, -5.3202814e-01f, -4.9346226e-01f, -4.5409340e-01f, -4.1411650e-01f, -3.7372491e-01f, -3.3310896e-01f, -2.9245535e-01f, -2.5194588e-01f, -2.1175675e-01f, -1.7205767e-01f, -1.3301112e-01f, -9.4771549e-02f, -5.7484843e-02f, -2.1287683e-02f, 1.3692919e-02f, 4.7340058e-02f, 7.9547286e-02f, 1.1021888e-01f, 1.3927005e-01f, 1.6662708e-01f, 1.9222736e-01f, 2.1601939e-01f, 2.3796269e-01f, 2.5802764e-01f, 2.7619535e-01f, 2.9245722e-01f, 3.0681485e-01f, 3.1927946e-01f, 3.2987154e-01f, 3.3862048e-01f, 3.4556398e-01f, 3.5074741e-01f, 3.5422349e-01f, 3.5605150e-01f, 3.5629687e-01f, 3.5503030e-01f, 3.5232738e-01f, 3.4826782e-01f, 3.4293488e-01f, 3.3641466e-01f, 3.2879567e-01f, 3.2016793e-01f, 3.1062266e-01f, 3.0025154e-01f, 2.8914624e-01f, 2.7739778e-01f, 2.6509622e-01f, 2.5232998e-01f, 2.3918559e-01f, 2.2574714e-01f, 2.1209598e-01f, 1.9831035e-01f, 1.8446516e-01f, 1.7063159e-01f, 1.5687700e-01f, 1.4326461e-01f, 1.2985344e-01f, 1.1669817e-01f, 1.0384899e-01f, 9.1351636e-02f, 7.9247288e-02f, 6.7572616e-02f, 5.6359809e-02f, 4.5636635e-02f, 3.5426527e-02f, 2.5748687e-02f, 1.6618228e-02f, 8.0463104e-03f, 4.0317649e-05f, -7.3959655e-03f, -1.4262161e-02f, -2.0561092e-02f, -2.6298566e-02f, -3.1483162e-02f, -3.6126003e-02f, -4.0240526e-02f, -4.3842256e-02f, -4.6948578e-02f, -4.9578514e-02f, -5.1752489e-02f, -5.3492129e-02f, -5.4820038e-02f, -5.5759601e-02f, -5.6334782e-02f, -5.6569938e-02f, -5.6489646e-02f, -5.6118533e-02f, -5.5481113e-02f, -5.4601658e-02f, -5.3504050e-02f, -5.2211665e-02f, -5.0747264e-02f, -4.9132895e-02f, -4.7389805e-02f, -4.5538362e-02f, -4.3598000e-02f, -4.1587163e-02f, -3.9523251e-02f, -3.7422612e-02f, -3.5300497e-02f, -3.3171058e-02f, -3.1047333e-02f, -2.8941268e-02f, -2.6863704e-02f, -2.4824409e-02f, -2.2832101e-02f, -2.0894464e-02f, -1.9018197e-02f, -1.7209040e-02f, -1.5471818e-02f, -1.3810488e-02f, -1.2228184e-02f, -1.0727265e-02f, -9.3093645e-03f, -7.9664271e-03f, -6.3887839e-03f, -4.6444070e-03f, -3.0082264e-03f, -1.6805470e-03f, -7.6002488e-04f, -2.4136060e-04f, -3.4723744e-05f, -0.0000000e+00f };

        public struct Trace
        {
            public int position;
            public int transmitter;
            public float[] data;
            public Trace(int p, float[] d, int transmitter_c)
            {
                position = p;
                transmitter = transmitter_c;
                data = d;
            }
            public Trace(Trace t0)//при копировании трассы не переносим данные
            {
                position = t0.position;
                transmitter = t0.transmitter;
                data = new float[t0.data.Length];
            }
        }

        public static SliceFormingType currentSliceFormingType;


        
        public static FuncProc.NoiseType curNoiseType = FuncProc.NoiseType.none;


        public static void OpenMultiFile(string path)
        {

            modelName = Path.GetFileNameWithoutExtension(path);
            string pathToDir = Path.GetDirectoryName(path);
            bool isRemovedDirectWave = false;
            bool isDeconvoled = false;

            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                numberOfTraces = reader.ReadInt32();
                step = reader.ReadInt32();
                maxtime = reader.ReadInt32();
                dx = reader.ReadSingle() / 1000;
                dy = reader.ReadSingle() / 1000;
                dt = ((double)1) / reader.ReadInt32();
                envSpeed = reader.ReadInt32();
                envAbs = reader.ReadDouble();
                zoneWidth = reader.ReadInt32();
                zoneHeight = reader.ReadInt32();
                numberOfSonograms = reader.ReadInt32();
                isRemovedDirectWave = reader.ReadInt32() > 0;
                isDeconvoled = reader.ReadInt32() > 0;
            }
            width = numberOfTraces;//у нас нет данных для промежуточных столбцов, т.е. за ширину принимаем кол-во трасмиттеров, настоящией размеры по горизонтили могут потребоваться для вычислений

            OpenSpeedmap(pathToDir + "\\speedmap.bmp");
            speedMapOst = new float[width, maxtime];

            tracesData = new Trace[numberOfSonograms * numberOfTraces];
            float[] temp;

            filename = Path.GetFileName(path);
            for (int z = 0; z < numberOfSonograms; z++)
            {
                using (BinaryReader reader = new BinaryReader(File.Open(pathToDir + '\\' + modelName + "_" + z + ".data", FileMode.Open)))
                {
                    for (int x = 0; x < numberOfTraces; x++)
                    {
                        temp = new float[maxtime];
                        for (int t = 0; t < maxtime; t++)
                        {
                            temp[t] = reader.ReadSingle();
                        }
                        tracesData[z * numberOfTraces + x] = new Trace(x, temp, z);
                    }
                }
            }
            if (!isRemovedDirectWave)
            {
                tracesData = RemoveDirectWave(tracesData, EmulateDirectWave(envSpeed, envAbs));
            }

            if (!isDeconvoled)
            {
                DeconvolveAllTraces();
            } else
            {
                ConvolveAllTraces();
            }
        }

        private static void OpenSpeedmap(string path)//открытие карты скоростей в пространстве
        {
            Bitmap bmp = new Bitmap(path);

            smWidth = bmp.Width;
            smHeight = bmp.Height;
            speedMap = new int[smWidth, smHeight];

            for (int x = 0; x < smWidth; x++)
            {
                for (int y = 0; y < smHeight; y++)
                {
                    var pc = bmp.GetPixel(x, y);
                    speedMap[x, y] = pc.B + pc.G * 256 + pc.R * 256 * 256;
                }
            }
        }
        public static void ConvolveAllTraces(double decA = Double.NegativeInfinity)
        {
            tracesDataDeconv = tracesData;
            tracesData = Deconvolve(tracesDataDeconv, -1, true);
        }
        public static void DeconvolveAllTraces(double decA = Double.NegativeInfinity)
        {
            tracesDataDeconv = Deconvolve(tracesData, decA);
        }

        public static Trace[] Deconvolve(Trace[] data, double decA = Double.NegativeInfinity, bool onlyRe = false)
        {
            var tracesDataDeconv = new Trace[numberOfSonograms * numberOfTraces];
            if (Double.IsNegativeInfinity(decA)) decA = curDecA;

            var impulseSpec = new Exocortex.DSP.Complex[4096];
            for (int i = 0; i < signal.Length; i++)
            {
                impulseSpec[i].Re = signal[i];
            }
            Exocortex.DSP.Fourier.FFT(impulseSpec, 4096, Exocortex.DSP.FourierDirection.Forward);

            var impMulti = new Exocortex.DSP.Complex[4096];
            if (decA >= 0)
            {
                var impulseSpecInv = new Exocortex.DSP.Complex[4096];
                for (int i = 0; i < impulseSpec.Length; i++)
                {
                    impulseSpecInv[i].Re = impulseSpec[i].Re;
                    impulseSpecInv[i].Im = -impulseSpec[i].Im;
                }

                for (int i = 0; i < impulseSpec.Length; i++)
                {
                    impMulti[i] = impulseSpecInv[i] / (Math.Pow(impulseSpec[i].Re, 2) + Math.Pow(impulseSpec[i].Im, 2) + Math.Pow(decA, 2));
                }
            }
            else
            {
                for (int i = 0; i < impulseSpec.Length; i++)
                {
                    impMulti[i] = impulseSpec[i];
                }
            }            

            Parallel.For(0, data.Length, tn =>
            {
                var t = data[tn];
                var tempColumn = new Exocortex.DSP.Complex[4096];
                for (int i = 0; i < Math.Min(4096, maxtime); i++)
                {
                    tempColumn[i].Re = t.data[i];
                }
                Fourier.FFT(tempColumn, 4096, FourierDirection.Forward);
                for (int i = 0; i < 4096; i++)
                {
                    tempColumn[i] = tempColumn[i] * impMulti[i];
                }
                Fourier.FFT(tempColumn, 4096, FourierDirection.Backward);

                Trace tConv = new Trace(t);

                for (int i = 0; i < Math.Min(4096, maxtime); i++)//обрезаем то, что вылезло за границы
                {
                    tConv.data[i] = (onlyRe ? (float)tempColumn[i].Re : (float)Math.Sqrt(tempColumn[i].Re * tempColumn[i].Re + tempColumn[i].Im * tempColumn[i].Im)) / 4096;
                }
                tracesDataDeconv[tn] = tConv;
            });

            return tracesDataDeconv;
        }

        public static float[,] Migration(float[,] slice)
        {
            //для каждой точки берем скорость
            
            float[,] newBmp = new float[numberOfTraces, maxtime];
            for (int t = 0; t < maxtime; t++)
            {
                int y;
                int t1 = t / 2;
                for (int trace = 0; trace < numberOfTraces; trace++)
                {
                    int speed = GetSpeedFromSpeedmap(trace * step, t1, out y);

                    float cnt = 0;
                    float sum = 0;

                    double tdt = t * dt;
                    double tdt2 = tdt * tdt;
                    for (int x = -diffWidth + 1; x < diffWidth; x++)
                    {
                        int pos = trace + x;
                        if (pos < 0 || pos >= numberOfTraces) continue;

                        float xi = x * dx * step;
                        int newT = (int)(Math.Sqrt(tdt2 + 4 * xi * xi / (speed * speed)) / dt);
                        if (newT >= maxtime) continue;
                        sum += slice[pos, newT];
                        cnt += 1;
                    }
                    sum /= cnt;
                    newBmp[trace, t] = sum;

                    //суммируем, усредняем
                }
            }
            return newBmp;
        }

        public static void MigrationAll()
        {
            Parallel.For(0, slices.Length, tn =>
            {
                slices[tn] = Migration(slices[tn]);
            });
        }

        public static int GetSpeedFromSpeedmap(int x, int time, out int y)
        {
            //бежим по пространству, смотрим на время. Когда достигает, делаем шаг назад
            double t = 0;
            y = 0;
            //time /= 2; // потому что входное время - отраженная волна (туда-обратно)
            while (t <= time && y < smHeight - 1)//до тех пор, пока не дошли до нужного времени или конца изображения
            {
                t += dy / speedMap[x, y] / dt;
                y++;
            }
            return speedMap[x, --y];
        }

        public static int GetEffectiveSpeedFromSpeedmap(int x, int time)
        {
            double tsum = 0;
            double temp = 0;
            int y = 0;
            time /= 2;
            //нужно идти по слоям и записывать время/скорость каждого проходимого слоя. Суммировать tk*v*v и tk
            int lastSpeed = speedMap[x, 0];
            double lastTime = 0;
            while (tsum <= time && y < smHeight - 1)//до тех пор, пока не дошли до нужного времени или конца изображения
            {
                if (speedMap[x, y] != lastSpeed)//конец слоя
                {
                    tsum += lastTime;
                    temp += lastTime * lastSpeed * lastSpeed;
                    lastTime = 0;
                    lastSpeed = speedMap[x, y];
                }
                else
                {
                    lastTime += dx / lastSpeed / dt;
                }
                y++;
            }
            if (lastTime != 0)
            {
                tsum += lastTime;
                temp += lastTime * lastSpeed * lastSpeed;
            }

            return (int)Math.Sqrt(temp / tsum);
        }


        public static void RefreshSlicesData()
        {
            RefreshSlicesData(currentSliceFormingType);
        }
        public static void RefreshSlicesData(SliceFormingType type)
        {
            var tData = useDeconvolvedData ? tracesDataDeconv : tracesData;
            if (tData == null || tData.Length == 0)
                tData = tracesData;

            currentSliceFormingType = type;

            slices = new float[numberOfSonograms][,];

            Parallel.For(0, numberOfSonograms, sliceNumber => 
            {

                float[,] sliceData = new float[numberOfTraces, maxtime];

                for (int x = 0; x < numberOfTraces; x++)
                {
                    Trace t = new Trace();
                    switch (type)
                    {
                        case SliceFormingType.ns:
                            t = tData.Where(e => e.position == x && e.transmitter == x).FirstOrDefault();//нулевого сдвига
                            break;
                        case SliceFormingType.opv:
                            t = tData.Where(e => e.position == x && e.transmitter == sliceNumber).FirstOrDefault();//общего пункта возбуждения
                            break;
                        case SliceFormingType.opp:
                            t = tData.Where(e => e.position == sliceNumber && e.transmitter == x).FirstOrDefault();//общего пункта приёма
                            break;
                        case SliceFormingType.ost:
                            t = tData.Where(e => e.position == sliceNumber * 2 - x && e.transmitter == x).FirstOrDefault();//общей средней точки
                            break;
                    };

                    if (t.data == null) continue;//для данной трассы данных не нашлось

                    for (int y = 0; y < maxtime; y++)//здесь нам нужно взять нужные трассы и собрать из них кадр
                    {
                        sliceData[x, y] = t.data[y];
                    }
                }
                slices[sliceNumber] = sliceData;
            });
        }


        public static float[,] GetSlice(int sliceNumber)
        {
            return slices[sliceNumber];
        }

        //public static void MakeReverse(float[, ,] curData)//делаем реверс заданного массива
        //{
        //    int currentmaxtime;
        //    int accuratewaitingtime;
        //    float[] temp;
        //    for (int z = 0; z < numberOfTraces; z++)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            temp = new float[maxtime + 1];

        //            for (int t = 0; t <= maxtime; t++)
        //            {
        //                if (curData[x, t, z] == 0)
        //                    continue;
        //                currentmaxtime = t;//зафиксировали время одного прохода
        //                accuratewaitingtime = currentmaxtime * 3;//нам нужно отмерить три полных прохода волны - после реверсирования эта точка станет нулём
        //                temp = new float[accuratewaitingtime];//создаем временный массив под диапазон, подвергающийся реверсированию
        //                for (int i = accuratewaitingtime - 1; i >= 0; i--)//проверить правильность, есть сомнения
        //                {
        //                    temp[i] = curData[x, accuratewaitingtime/* - 1*/ - i, z];
        //                }

        //                for (int i = 0; i < accuratewaitingtime; i++)//записываем результат реверсирования обратно в данные
        //                {
        //                    curData[x, i, z] = temp[i];
        //                }
        //                for (int i = accuratewaitingtime; i < maxtime; i++)//забиваем остаток нулями на всякий случай
        //                {
        //                    curData[x, i, z] = 0;
        //                }

        //                break;
        //            }
        //        }
        //    }
        //}

        //DEBUG - для поиска аномально высоких значений
        //public static void CheckDataForAnomalMaxValues(float maxTreshold, float minTreshold)
        //{
        //    float max = float.MinValue;
        //    float min = float.MaxValue;

        //    for (int x = 0; x < width; x++)
        //    {
        //        for (int y = 0; y <= maxtime; y++)
        //        {
        //            for (int z = 0; z < depth; z++)
        //            {
        //                if (max < data[x, y, z])
        //                {
        //                    max = data[x, y, z];
        //                }
        //                if (min > data[x, y, z])
        //                {
        //                    min = data[x, y, z];
        //                }
        //                if (data[x, y, z] > maxTreshold)
        //                    Console.Write("Слишком большие + значения!!!");
        //                if (data[x, y, z] < minTreshold)
        //                    Console.Write("Слишком большие - значения!!!");
        //            }
        //        }
        //    }
        //}


        public static float[,] AvgSlices()
        {
            var avgSlice = new float[numberOfTraces, maxtime];
            for (int z = 0; z < numberOfSonograms; z++)
            {
                for (int x = 0; x < numberOfTraces; x++)
                {
                    for (int t = 0; t < maxtime; t++)
                    {
                        avgSlice[currentSliceFormingType == SliceFormingType.ost ? z : x, t] += slices[z][x, t] / numberOfSonograms;
                    }
                }
            }

            return avgSlice;
        }

        public static float[,] SumSlices()
        {
            var sumSlice = new float[numberOfTraces, maxtime];
            for (int z = 0; z < numberOfSonograms; z++)
            {
                for (int x = 0; x < numberOfTraces; x++)
                {
                    for (int t = 0; t < maxtime; t++)
                    {
                        sumSlice[currentSliceFormingType == SliceFormingType.ost ? z : x, t] += slices[z][x, t];
                    }
                }
            }

            return sumSlice;
        }

        public static Trace[] RemoveDirectWave(Trace[] data, Trace[] directWave)
        {
            var cleanData = (Trace[])data.Clone();
            for (int k = 0; k < numberOfSonograms; k++)
            {
                for (int i = 0; i < numberOfTraces; i++)
                {
                    int nTrace = k * numberOfTraces + i;
                    for (int t = 0; t < maxtime; t++)
                    {
                        cleanData[nTrace].data[t] = data[nTrace].data[t] - directWave[nTrace].data[t];
                    }
                }
            }

            return cleanData;
        }

        public static Trace[] EmulateDirectWave(float envSpeed, double envAbs)
        {
            var directWave = new Trace[numberOfSonograms * numberOfTraces];
            var abs = (float)Math.Exp(-envAbs * step * dx * 100); // поглощение всегда считается с дистанцией шага трансдьюсеров
            for (int k = 0; k < numberOfSonograms; k++) // для каждой сонограммы
            {
                float val = 1;
                // эмулируем прямую волну
                for (int x = 0; x < numberOfTraces; x++) // удаление от источника
                {
                    var d = x * dx * step;
                    var t = d / envSpeed / dt;
                    val *= abs;
                    foreach (int pos in new[]{k - x, k + x})
                    {
                        if (pos < 0 || pos >= numberOfTraces) continue; // вышли за границы по ширине
                        int j = k * numberOfTraces + pos;
                        directWave[j] = new Trace(pos, new float[maxtime], k);

                        int newT = (int)Math.Round(t);
                        if (newT > directWave[j].data.Length - 1) continue; // вышли за границы по высоте (времени)
                        directWave[j].data[newT] = val;
                    }
                }
            }

            directWave = Deconvolve(directWave, -1, true);

            // обнуляем трассу с одинаковым номер источника и приемника
            for (int k = 0; k < numberOfTraces; k++)
            {
                directWave[k * numberOfTraces + k].data = new float[maxtime];
            }

            return directWave;
        }


        public static void WholeApplyFilter(float[] filter)
        {
            Parallel.For(0, numberOfSonograms, k =>
            {
                for (int i = 0; i < numberOfTraces; i++)
                {
                    tracesData[k * numberOfTraces + i].data = FuncProc.ApplyFilter(tracesData[k * numberOfTraces + i].data, filter);
                }
            });
            DeconvolveAllTraces();
            RefreshSlicesData();
        }

        public static float[,] CreateSpeedMatrixAll(int dV = 10, int maxV = 20000)
        {
            int vWidth = maxV / dV;
            var vData = new float[vWidth, maxtime];
            var vDataArr = new float[numberOfSonograms][,];
            Parallel.For(0, numberOfTraces, tn =>
            {
                var vDataCur = CreateSpeedMatrix(slices[tn], tn, dV, maxV);
                var allDataCur = new Tuple<int, int, float>[vWidth * maxtime];
                for (int i = 0; i < vWidth; i++)
                {
                    for (int t = 0; t < maxtime; t++)
                    {
                        allDataCur[i * maxtime + t] = new Tuple<int, int, float>(i, t, vDataCur[i, t]);
                    }
                }

                var maxData = new float[maxtime];
                for (int t = 0; t < maxtime; t++)
                {
                    var max = (float)Double.NegativeInfinity;
                    for (int i = 0; i < vDataCur.GetLength(0); i++)
                    {
                        if (vDataCur[i, t] > max)
                        {
                            max = vDataCur[i, t];
                            maxData[t] = i * dV;
                        }
                    }
                }
                //maxData = FuncProc.AntiSpike(maxData);
                
                for (int t = 0; t < maxtime; t++)
                {
                    speedMapOst[tn, t] = maxData[t];
                    if (speedMapOst[tn, t] == 0) speedMapOst[tn, t] = 1;
                }
            });
            return speedMapOst;
        }

        public static float[,] CreateSpeedMatrix(float[,] data, int avgX, int dV = 10, int maxV = 20000)
        {
            int vWidth = maxV / dV;
            var vData = new float[vWidth, maxtime];
            var newData = new float[numberOfTraces, maxtime];
            //int tWindow = diffWidth / 2;
            int tWindow = 1;
            for (int vi = 1; vi < vWidth; vi++)
            {
                int speed = vi * dV;
                int speed2 = speed * speed;
                for (int t0 = 0; t0 < maxtime; t0++)//для каждого времени ОСТ
                {
                    float cc = 0;
                    for (int k = -tWindow + 1; k < tWindow; k++)
                    {
                        int t = t0 + k;
                        if (t < 0 || t >= maxtime) continue;
                        float first = 0;
                        float second = 0;
                        double tdt2 = t * t * dt * dt;
                        for (int x = -diffWidth + 1; x < diffWidth; x++)
                        {
                            int pos = avgX + x;
                            if (pos < 0 || pos >= numberOfTraces) continue;

                            float xi = x * dx * step * 2; // 2 - потому что идем в 2 стороны от avgX
                            int newT = (int)(Math.Sqrt(tdt2 + xi * xi / speed2) / dt);
                            if (newT >= maxtime) continue;

                            float val = data[pos, newT];
                            first += val;
                            second += val * val;
                        }
                        cc += first * first - second;
                    }
                    cc /= 2;
                    vData[vi, t0] = cc;


                    //vData[vi, t] = first / cnt;
                }

                
            }
            return vData;
        }

        public static float[,] KinematicFixes(float[,] data, int avgX, int speed = -1)
        {
            float[,] newData = new float[numberOfTraces, maxtime];
            bool stop = false;
            int y;
            for (int t = 1; t < maxtime; t++)//для каждого времени ОСТ
            {
                stop = false;
                if (speed < 0) speed = (int)(GetSpeedFromSpeedmap(avgX * step, t / 2, out y));
                //if (speed < 0) speed = (int)speedMapOst[avgX, t];
                if (currentSliceFormingType == SliceFormingType.opv)
                {
                    double tdt2 = t * t * dt * dt;
                    for (int i = 0; i < numberOfTraces; i++)//для каждого приемника
                    {
                        float ld = Math.Abs(i - avgX); // удаление вершины годографа от источника
                        float lx = ld * dx * step; // удаление в реальных цифрах
                        float firstPart = (float)Math.Sqrt(tdt2 + lx * lx / (speed * speed)); // первое слагаемое в формуле годографа ОПВ


                        int newT = (int)((firstPart) / dt);
                        if (newT >= maxtime) continue;

                        newData[i, t] = data[i, newT];
                    }
                }
                else
                {
                    newData[avgX, t] = data[avgX, t];//столбец средней точки меняться не должен
                    for (int x = 1; stop == false && x < diffWidth; x++)//идем в обе стороны от общей средней точки
                    {
                        stop = true;
                        float xi = x * dx * step * 2; // 2 - потому что идем в 2 стороны от avgX
                        int newT = (int)(Math.Sqrt(t * t * dt * dt + xi * xi / (speed * speed)) / dt);
                        
                        foreach (int pos in new int[] { avgX - x, avgX + x })
                        {
                            if ((pos >= 0) && pos < numberOfTraces && (newT < maxtime))
                            {
                                newData[pos, t] = data[pos, newT];
                                stop = false;
                            }
                        }
                    }
                }
            }
            return newData;
        }

        public static void KinematicFixesAll(int speed = -1)
        {
            Parallel.For(0, numberOfSonograms, tn =>
            {
                slices[tn] = KinematicFixes(slices[tn], tn, speed);
            });
        }

        public static void KinematicFixesTraces()
        {
            var newTraces = new Trace[numberOfSonograms * numberOfTraces];
            var clearSlices = new float[numberOfSonograms][,];
            Parallel.For(0, numberOfSonograms, k =>
            {
                var slice = new float[numberOfTraces, maxtime];
                for (int i = 0; i < numberOfTraces; i++)
                {
                    for (int j = 0; j < maxtime; j++)
                    {
                        slice[i, j] = tracesData[k * numberOfTraces + i].data[j];
                    }
                }
                slice = KinematicFixes(slice, k);
                for (int i = 0; i < width; i++)
                {
                    newTraces[k * numberOfTraces + i] = new Trace(tracesData[k * numberOfTraces + i]);
                    var trace = new float[maxtime];
                    for (int j = 0; j < maxtime; j++)
                    {
                        trace[j] = slice[i, j];
                    }
                    newTraces[k * numberOfTraces + i].data = trace;
                }
            });
            tracesData = newTraces;
            DeconvolveAllTraces();
            RefreshSlicesData();
        }

        public static void AddNoise(double f = 0.1, int rndSize = 10)
        {
            Parallel.For(0, numberOfSonograms, k =>
            {
                slices[k] = FuncProc.AddNoise(slices[k], curNoiseType, f, rndSize);
            });
        }


        public static float[,] MigrationOpv(float[,] data, int posS, int speedIn = -1)
        {
            float[,] newData = new float[numberOfTraces, maxtime];
            int speed = speedIn;
            int y;
            for (int t = 0; t < maxtime; t++)//для каждого времени ОСТ
            {
                int t1 = t / 2;
                double tdt = t1 * dt;
                double tdt2 = tdt * tdt;
                for (int i = 0; i < numberOfTraces; i++)//для каждого приемника
                {
                    if (speedIn < 0) speed = (int)(GetSpeedFromSpeedmap(i * step, t1, out y));
                    //if (speed < 0) speed = (int)speedMapOst[i, t];
                    int cnt = 0;
                    float sum = 0;
                    float ld = Math.Abs(i - posS); // удаление вершины годографа от источника
                    float lx = ld * dx * step; // удаление в реальных цифрах
                    float firstPart = (float)Math.Sqrt(tdt2 + lx * lx / (speed * speed)); // первое слагаемое в формуле годографа ОПВ
                    //float firstPart = 0;
                    for (int x = -diffWidth + 1; x < diffWidth; x++) //идем в обе стороны от центра годографа
                    {
                        int pos = i + x;
                        if (pos < 0 || pos >= numberOfTraces) continue;

                        float xi = x * dx * step;
                        float secondPart = (float)Math.Sqrt(tdt2 + xi * xi / (speed * speed)); // второе слагаемое в формуле годографа ОПВ
                        int newT = (int)((firstPart + secondPart) / dt);
                        if (newT >= maxtime) continue;
                        float k = x == 0 ? 2 : Math.Min(1, Math.Abs(1 / ((float)x / 3)));

                        float val = data[pos, newT];
                        sum += val;
                        cnt++;
                    }

                    newData[i, t] = cnt > 0 ? sum / cnt : 0;
                }
            }
            return newData;
        }

        public static void MigrationOpvAll(int speed = -1)
        {
            Parallel.For(0, slices.Length, tn =>
            {
                slices[tn] = MigrationOpv(slices[tn], tn, speed);
            });
        }

        public static float[,] TimeToDist(float[,] data)
        {
            float[,] newData = new float[numberOfTraces * step, smHeight];
            int time = maxtime;
            int tStart, tEnd;
            for (int x = 0; x < numberOfTraces; x++)//для каждого приемника
            {
                int t = 0;
                for (int y = 0; y < smHeight; y++)//для каждого приемника
                {
                    tStart = t;
                    t += 2 * (int)Math.Round(dy / speedMap[x*step, y] / dt);
                    tEnd = t;
                    if (tEnd > maxtime) break;
                    float sum = 0;
                    for (int tt = tStart; tt < tEnd; tt++)
                    {
                        sum += data[x, tt];
                    }
                    newData[x * step, y] = sum / (tEnd - tStart);
                }
            }
            // расширяем изображения для заполнения промежутков между датчиками (по step)
            for (int y = 0; y < smHeight; y++)
            {
                for (int x = 0; x < numberOfTraces - 1; x++)
                {
                    float vStart = newData[x * step, y];
                    float vEnd = newData[(x + 1) * step, y];
                    float diff = (vEnd - vStart) / step;
                    for (int s = 0; s < step; s++)
                    {
                        newData[x * step + s, y] = vStart + diff * s;
                    }
                }
            }
            return newData;
        }

        public static float[,] GetFloatSpeedMap()
        {
            var res = new float[smWidth, smHeight];
            for (int i = 0; i < smWidth; i++)
            {
                for (int j = 0; j < smHeight; j++)
                {
                    res[i, j] = speedMap[i, j];
                }
            }
            return res;
        }

        public static float[,] Amplify(float[,] data, AmplifyType type, double k = 1)
        {
            var w = data.GetLength(0);
            var h = data.GetLength(1);
            var newData = new float[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    switch (type)
                    {
                        case AmplifyType.linear:
                            newData[i, j] = data[i, j] * (1 + j * (float)k);
                            break;
                        case AmplifyType.log:
                            newData[i, j] = data[i, j] * (1 + (float)Math.Log(1 + j, k));
                            break;
                        case AmplifyType.pow:
                            newData[i, j] = data[i, j] * (1 + (float)Math.Pow(k, j));
                            break;
                    }
                }
            } 
            return newData;
        }

        public static void WholeAmplify(AmplifyType type, double k = 1)
        {
            Parallel.For(0, slices.Length, tn =>
            {
                slices[tn] = Amplify(slices[tn], type, k);
            });
        }



        public static void saveDataFile(float[,] data, string path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
            {
                for (int x = 0; x < numberOfTraces; x++)
                {
                    for (int t = 0; t < maxtime; t++)
                    {
                        writer.Write(data[x, t]);
                    }
                }
            }
        }

        public static void saveInfoFile(string path, int cntFiles)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                writer.Write(numberOfTraces); //количество трансмиттеров
                writer.Write(step); //шаг, с которым они расположены, начиная с 0
                writer.Write(maxtime);
                writer.Write((float)(1000 * dx)); //мм в 1 пикселе по dx
                writer.Write((float)(1000 * dy)); //мм в 1 пикселе по dy
                writer.Write((int)(1 / dt)); //частота дискретизации
                writer.Write(envSpeed); // скорость внешней среды
                writer.Write(envAbs); // поглощение внешней среды
                writer.Write(zoneWidth); // ширина области
                writer.Write(zoneHeight); // высота области
                writer.Write(cntFiles); // кол-во сонограмм
                writer.Write(1); // прямая волна уже убрана
                writer.Write(useDeconvolvedData ? 1 : 0); // деконволированны ли трассы?
            }
        }

        public static void saveSpeedmap(string path)
        {
            Bitmap bmp = new Bitmap(smWidth, smHeight);

            for (int x = 0; x < smWidth; x++)
            {
                for (int y = 0; y < smHeight; y++)
                {
                    int speed = speedMap[x, y];
                    int B = speed % 256;
                    speed -= B;
                    speed /= 256;
                    int G = speed % 256;
                    speed -= G;
                    speed /= 256;
                    int R = speed; 
                    bmp.SetPixel(x, y, Color.FromArgb(255, R, G, B));
                }
            }

            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
        }
    }
}
