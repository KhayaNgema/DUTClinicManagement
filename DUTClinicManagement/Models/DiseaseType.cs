using System.ComponentModel.DataAnnotations;

public enum DiseaseType
{
    [Display(Name = "Chronic Disease")]
    Chronic,

    [Display(Name = "Acute Disease")]
    Acute,

    [Display(Name = "Infectious Disease")]
    Infectious,

    [Display(Name = "Non-Infectious Disease")]
    NonInfectious,

    [Display(Name = "Genetic or Hereditary Disorder")]
    Genetic,

    [Display(Name = "Autoimmune Disease")]
    Autoimmune,

    [Display(Name = "Mental or Neurological Disorder")]
    MentalOrNeurological,

    [Display(Name = "Lifestyle Disease")]
    Lifestyle,

    [Display(Name = "Occupational Disease")]
    Occupational,

    [Display(Name = "Deficiency Disease")]
    Deficiency,

    [Display(Name = "Genetic Or Hereditary")]
    GeneticOrHereditary
}
