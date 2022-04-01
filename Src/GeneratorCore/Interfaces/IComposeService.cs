﻿using System.Threading.Tasks;
using GeneratorCore.Dto;
using TripleSix.Core.Services;

namespace GeneratorCore.Interfaces
{
    public interface IComposeService : IService
    {
        Task Write(CardModelDto model, string outputPath, CardSetDto setConfig);

        Task Write(ComposeDataDto input, string outputFilename, CardSetDto setConfig);
    }
}
