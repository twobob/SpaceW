﻿#region License
// Procedural planet generator.
//  
// Copyright (C) 2015-2018 Denis Ovchinnikov [zameran] 
// All rights reserved.
//  
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the copyright holders nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
//  
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION)HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//  
// Creation Date: 2017.09.04
// Creation Time: 7:29 PM
// Creator: zameran
#endregion

using System;

using UnityEngine;

namespace SpaceEngine.SciptableObjects
{
    [Serializable]
    [CreateAssetMenu(fileName = "GenerationShadersCoreSettings", menuName = "Create Generation Shaders Core Settings")]
    public class GenerationShadersCoreSettings : ScriptableObject
    {
        [Header("I/O")]
        public ComputeShader WriteData;
        public ComputeShader ReadData;

        [Header("Atmosphere Precomputation")]
        public ComputeShader CopyInscatter1;
        public ComputeShader CopyInscatterN;
        public ComputeShader CopyIrradiance;
        public ComputeShader Inscatter1;
        public ComputeShader InscatterN;
        public ComputeShader InscatterS;
        public ComputeShader Irradiance1;
        public ComputeShader IrradianceN;
        public ComputeShader Transmittance;

        [Header("Ocean Precomputation")]
        public Shader Fourier;
        public ComputeShader Variance;
    }
}