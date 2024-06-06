using Models.DCAPST.Interfaces;
using System;

namespace Models.DCAPST
{
    /// <summary>
    /// Models the parameters of the leaf necessary to calculate photosynthesis
    /// </summary>
    public class TemperatureResponse
    {
        private const double ABSOLUTE_0C = 273;
        private const double ABSOLUTE_25C = 298.15;
        private const double ABSOLUTE_25C_X_GAS_CONSTANT = ABSOLUTE_25C * UNIVERSAL_GAS_CONSTANT;
        private const double UNIVERSAL_GAS_CONSTANT = 8.314;

        /// <summary>
        /// A collection of parameters as valued at 25 degrees Celsius
        /// </summary>
        private ParameterRates rateAt25;

        /// <summary>
        /// The parameters describing the canopy
        /// </summary>
        private readonly ICanopyParameters canopy;

        /// <summary>
        /// The static parameters describing the assimilation pathway
        /// </summary>
        private readonly IPathwayParameters pathway;

        /// <summary>
        /// Number of photons that reached the leaf
        /// </summary>
        private double photoncount;

        /// <summary>
        /// The leaf temperature.
        /// </summary>
        private double leafTemperature;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="pathway"></param>
        public TemperatureResponse(ICanopyParameters canopy, IPathwayParameters pathway)
        {
            this.canopy = canopy;
            this.pathway = pathway;
        }

        /// <summary>
        /// Sets rates and photon count.
        /// </summary>
        /// <param name="rates"></param>
        /// <param name="photons"></param>
        public void SetConditions(ParameterRates rates, double photons)
        {
            rateAt25 = rates;
            photoncount = photons;

            RecalculateParams();
        }

        /// <summary>
        /// The current leaf temperature
        /// </summary>
        public double LeafTemperature 
        { 
            get
            {
                return leafTemperature;
            }

            set
            {
                if (leafTemperature != value)
                {
                    leafTemperature = value;
                    RecalculateParams();
                }
            }
        }

        /// <summary>
        /// Maximum rate of rubisco carboxylation at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double VcMaxT { get; private set; }

        /// <summary>
        /// Leaf respiration at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double RdT { get; private set; }

        /// <summary>
        /// Maximum rate of electron transport at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double JMaxT { get; private set; }

        /// <summary>
        /// Maximum PEP carboxylase activity at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double VpMaxT { get; private set; }

        /// <summary>
        /// Mesophyll conductance at the current leaf temperature (mol CO2 m^-2 ground s^-1 bar^-1)
        /// </summary>
        public double GmT { get; private set; }

        /// <summary>
        /// Michaelis-Menten constant of Rubsico for CO2 (microbar)
        /// </summary>
        public double Kc { get; private set; }

        /// <summary>
        /// Michaelis-Menten constant of Rubsico for O2 (microbar)
        /// </summary>
        public double Ko { get; private set; }

        /// <summary>
        /// Ratio of Rubisco carboxylation to Rubisco oxygenation
        /// </summary>
        public double VcVo { get; private set; }

        /// <summary>
        /// Michaelis-Menten constant of PEP carboxylase for CO2 (micro bar)
        /// </summary>
        public double Kp { get; private set; }

        /// <summary>
        /// Electron transport rate
        /// </summary>
        public double J { get; private set; }

        /// <summary>
        /// Relative CO2/O2 specificity of Rubisco (bar bar^-1)
        /// </summary>
        public double Sco { get; private set; }

        /// <summary>
        /// Half the reciprocal of the relative rubisco specificity
        /// </summary>
        public double Gamma { get; private set; }

        /// <summary>
        /// Mesophyll respiration
        /// </summary>
        public double GmRd { get; private set; }

        private void RecalculateParams()
        {
            var leafTemperaturePlus0C = leafTemperature + ABSOLUTE_0C;
            var leafTemperaturePlus0CMinus25C = leafTemperaturePlus0C - ABSOLUTE_25C;

            VcMaxT = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, rateAt25.VcMax, pathway.RubiscoActivity.Factor);
            RdT = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, rateAt25.Rd, pathway.Respiration.Factor);
            JMaxT = ValueOptimum(leafTemperature, rateAt25.JMax, pathway.ElectronTransportRateParams);
            VpMaxT = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, rateAt25.VpMax, pathway.PEPcActivity.Factor);
            GmT = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, rateAt25.Gm, pathway.MesophyllCO2ConductanceParams.Factor);
            Kc = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, pathway.RubiscoCarboxylation.At25, pathway.RubiscoCarboxylation.Factor);
            Ko = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, pathway.RubiscoOxygenation.At25, pathway.RubiscoOxygenation.Factor);
            VcVo = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, pathway.RubiscoCarboxylationToOxygenation.At25, pathway.RubiscoCarboxylationToOxygenation.Factor);
            Kp = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, pathway.PEPc.At25, pathway.PEPc.Factor);
            
            UpdateElectronTransportRate();
            
            Sco = Ko / Kc * VcVo;
            Gamma = 0.5 / Sco;
            GmRd = RdT * 0.5;
        }

        /// <summary>
        /// Uses an exponential function to model temperature response parameters
        /// </summary>
        /// <remarks>
        /// See equation (1), A. Wu et al (2018) for details
        /// </remarks>
        private static double Value(
            double leafTemperaturePlus0C, 
            double leafTemperaturePlus0CMinus25C,
            double P25, 
            double tMin
        )
        {
            var numerator = tMin * leafTemperaturePlus0CMinus25C;
            var denominator = ABSOLUTE_25C_X_GAS_CONSTANT * leafTemperaturePlus0C;

            return P25 * Math.Exp(numerator / denominator);
        }

        /// <summary>
        /// Uses a normal distribution to model parameters with an apparent optimum in temperature response
        /// </summary>
        /// /// <remarks>
        /// See equation (2), A. Wu et al (2018) for details
        /// </remarks>
        private static double ValueOptimum(double temp, double P25, LeafTemperatureParameters p)
        {
            double tMin = p.TMin;
            double tOpt = p.TOpt;
            double tOptMinusTMin = tOpt - tMin;

            double alpha = Math.Log(2) / Math.Log((p.TMax - tMin) / (tOptMinusTMin));
            double twoTimesAlpha = 2 * alpha;
            double tempMinusTMin = temp - tMin;
            double numerator = 2 * Math.Pow(tempMinusTMin, alpha) * Math.Pow(tOptMinusTMin, alpha) - Math.Pow(tempMinusTMin, twoTimesAlpha);
            double denominator = Math.Pow(tOptMinusTMin, twoTimesAlpha);
            double funcT = P25 * Math.Pow(numerator / denominator, p.Beta) / p.C;

            return funcT;
        }

        /// <summary>
        /// Calculates the electron transport rate of the leaf
        /// </summary>
        private void UpdateElectronTransportRate()
        {
            var factor = photoncount * (1.0 - pathway.SpectralCorrectionFactor) / 2.0;
            J = (factor + JMaxT - Math.Pow(Math.Pow(factor + JMaxT, 2) - 4 * canopy.CurvatureFactor * JMaxT * factor, 0.5))
            / (2 * canopy.CurvatureFactor);
        }
    }
}
