using DUTClinicManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DUTClinicManagement.Services
{
    public class EmergencyPriorityService
    {
        private readonly Dictionary<Priority, List<string>> _keywordDictionary = new()
        {
            [Priority.VeryHigh] = new List<string> { "unconscious", "not breathing", "seizure", "cardiac arrest", "stroke", "severe bleeding", "chest pain" },
            [Priority.High] = new List<string> { "difficulty breathing", "broken bone", "allergic reaction", "intense pain", "burn", "high fever", "vomiting blood" },
            [Priority.Medium] = new List<string> { "fever", "persistent cough", "mild pain", "infection", "rash", "vomiting", "diarrhea" },
            [Priority.Low] = new List<string> { "headache", "minor cut", "sprain", "cold", "fatigue", "nausea", "back pain" }
        };

        public Priority AnalyzePriority(string emergencyDetails)
        {
            if (string.IsNullOrWhiteSpace(emergencyDetails))
                return Priority.Low;

            string normalized = emergencyDetails.ToLower();

            foreach (var level in Enum.GetValues<Priority>().OrderBy(p => p)) 
            {
                if (_keywordDictionary[level].Any(keyword => normalized.Contains(keyword)))
                {
                    return level;
                }
            }

            return Priority.Low;
        }
    }
}
