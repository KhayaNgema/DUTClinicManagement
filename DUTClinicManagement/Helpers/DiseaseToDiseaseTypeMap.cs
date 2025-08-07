using DUTClinicManagement.Models;
using System.Collections.Generic;

public static class DiseaseToDiseaseTypeMap
{
    public static readonly Dictionary<Disease, DiseaseType> Map = new()
    {
        { Disease.Diabetes, DiseaseType.Chronic },
        { Disease.Hypertension, DiseaseType.Chronic },
        { Disease.Asthma, DiseaseType.Chronic },
        { Disease.ChronicKidneyDisease, DiseaseType.Chronic },
        { Disease.ChronicObstructivePulmonaryDisease, DiseaseType.Chronic },
        { Disease.Osteoarthritis, DiseaseType.Chronic },
        { Disease.Epilepsy, DiseaseType.Chronic },

        { Disease.Influenza, DiseaseType.Acute },
        { Disease.Pneumonia, DiseaseType.Acute },
        { Disease.DengueFever, DiseaseType.Acute },
        { Disease.Gastroenteritis, DiseaseType.Acute },
        { Disease.Appendicitis, DiseaseType.Acute },
        { Disease.Malaria, DiseaseType.Acute },

        { Disease.Tuberculosis, DiseaseType.Infectious },
        { Disease.HIV_AIDS, DiseaseType.Infectious },
        { Disease.COVID_19, DiseaseType.Infectious },
        { Disease.HepatitisB, DiseaseType.Infectious },
        { Disease.HepatitisC, DiseaseType.Infectious },
        { Disease.Measles, DiseaseType.Infectious },
        { Disease.Syphilis, DiseaseType.Infectious },
        { Disease.TyphoidFever, DiseaseType.Infectious },

        { Disease.Cancer, DiseaseType.NonInfectious },
        { Disease.Stroke, DiseaseType.NonInfectious },
        { Disease.HeartDisease, DiseaseType.NonInfectious },
        { Disease.Alzheimers, DiseaseType.NonInfectious },
        { Disease.Lupus, DiseaseType.NonInfectious },
        { Disease.RheumatoidArthritis, DiseaseType.NonInfectious },

        { Disease.SickleCellAnemia, DiseaseType.GeneticOrHereditary },
        { Disease.DownSyndrome, DiseaseType.GeneticOrHereditary },
        { Disease.CysticFibrosis, DiseaseType.GeneticOrHereditary },
        { Disease.Hemophilia, DiseaseType.GeneticOrHereditary },
        { Disease.HuntingtonsDisease, DiseaseType.GeneticOrHereditary },

        { Disease.Type1Diabetes, DiseaseType.Autoimmune },
        { Disease.MultipleSclerosis, DiseaseType.Autoimmune },
        { Disease.Lupus, DiseaseType.Autoimmune },
        { Disease.Psoriasis, DiseaseType.Autoimmune },
        { Disease.RheumatoidArthritis, DiseaseType.Autoimmune },

        { Disease.Depression, DiseaseType.MentalOrNeurological },
        { Disease.AnxietyDisorders, DiseaseType.MentalOrNeurological },
        { Disease.Schizophrenia, DiseaseType.MentalOrNeurological },
        { Disease.ParkinsonsDisease, DiseaseType.MentalOrNeurological },
        { Disease.Alzheimers, DiseaseType.MentalOrNeurological },
        { Disease.Epilepsy, DiseaseType.MentalOrNeurological },

        { Disease.Obesity, DiseaseType.Lifestyle },
        { Disease.Type2Diabetes, DiseaseType.Lifestyle },
        { Disease.HeartDisease, DiseaseType.Lifestyle },
        { Disease.AlcoholicLiverDisease, DiseaseType.Lifestyle },
        { Disease.LungCancer, DiseaseType.Lifestyle },

        { Disease.Asbestosis, DiseaseType.Occupational },
        { Disease.CarpalTunnelSyndrome, DiseaseType.Occupational },
        { Disease.OccupationalAsthma, DiseaseType.Occupational },
        { Disease.Silicosis, DiseaseType.Occupational },
        { Disease.NoiseInducedHearingLoss, DiseaseType.Occupational },

        { Disease.Rickets, DiseaseType.Deficiency },
        { Disease.Scurvy, DiseaseType.Deficiency },
        { Disease.Anemia, DiseaseType.Deficiency },
        { Disease.Goiter, DiseaseType.Deficiency },
    };
}
