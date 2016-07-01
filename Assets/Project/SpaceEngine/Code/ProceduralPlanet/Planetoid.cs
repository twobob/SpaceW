﻿#region License
// Procedural planet generator.
// 
// Copyright (C) 2015-2016 Denis Ovchinnikov [zameran] 
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
// Creation Date: Undefined
// Creation Time: Undefined
// Creator: zameran
#endregion

using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using Amib;
using Amib.Threading;

public static class PlanetoidExtensions
{
    public static List<string> GetKeywords(this Planet planet)
    {
        List<string> Keywords = new List<string>();

        if (planet != null)
        {
            if (planet.Atmosphere != null)
            {
                if (planet.AtmosphereEnabled)
                {
                    if (planet.Atmosphere.Sun_1 != null)
                        Keywords.Add("LIGHT_1");

                    if (planet.Atmosphere.Sun_2 != null)
                        Keywords.Add("LIGHT_2");

                    if (planet.Atmosphere.Sun_1 != null && planet.Atmosphere.Sun_2 != null)
                        Keywords.Remove("LIGHT_1");

                    if (planet.Atmosphere.Sun_3 != null)
                        Keywords.Add("LIGHT_3");

                    if (planet.Atmosphere.Sun_4 != null)
                        Keywords.Add("LIGHT_4");

                    if (planet.Atmosphere.Sun_1 != null && planet.Atmosphere.Sun_2 != null)
                        if (planet.Atmosphere.Sun_3 != null && planet.Atmosphere.Sun_4 != null)
                            Keywords.Remove("LIGHT_2");

                    if (planet.Atmosphere.Sun_3 != null && planet.Atmosphere.Sun_4 != null)
                        Keywords.Remove("LIGHT_3");

                    if (planet.Atmosphere.eclipseCasters.Count == 0)
                    {
                        Keywords.Add("ECLIPSES_OFF");
                    }
                    else
                    {
                        Keywords.Add("ECLIPSES_ON");
                    }

                    if (planet.Atmosphere.shineCasters.Count == 0)
                    {
                        Keywords.Add("SHINE_OFF");
                    }
                    else
                    {
                        Keywords.Add("SHINE_ON");
                    }

                    Keywords.Add("ATMOSPHERE_ON");
                }
                else
                {
                    Keywords.Add("ATMOSPHERE_OFF");
                }
            }
            else
            {
                Debug.Log("Planet: GetKeywords problem!");
            }
        }

        return Keywords;
    }
}

public sealed class Planetoid : Planet, IPlanet
{
    public bool GetData = false;

    public List<Quad> MainQuads = new List<Quad>();
    public List<Quad> Quads = new List<Quad>();

    public Shader ColorShader;
    public ComputeShader CoreShader;

    public int DispatchSkipFramesCount = 8;

    public Mesh PrototypeMesh;

    public QuadStorage Cache = null;
    public NoiseParametersSetter NPS = null;

    public QuadDrawAndCull DrawAndCull = QuadDrawAndCull.CullBeforeDraw;
    public QuadCullingMethod CullingMethod = QuadCullingMethod.Custom;
    public QuadLODDistanceMethod LODDistanceMethod = QuadLODDistanceMethod.ClosestCorner;

    public TCCommonParametersSetter tccps;

    public Plane[] FrustumPlanes;

    public MaterialPropertyBlock QuadAtmosphereMPB;

    public void QuadDispatchStarted(Quad q)
    {

    }

    public void QuadDispatchReady(Quad q)
    {

    }

    public void QuadGPUGetDataReady(Quad q)
    {

    }

    protected override void Awake()
    {
        base.Awake();

        if (Atmosphere != null)
        {
            if (Atmosphere.planetoid == null)
                Atmosphere.planetoid = this;

            Atmosphere.Origin = Origin;
        }

        if (Cloudsphere != null)
        {
            if (Cloudsphere.planetoid == null)
                Cloudsphere.planetoid = this;
        }

        if (CameraHelper.Main() != null)
            FrustumPlanes = GeometryUtility.CalculateFrustumPlanes(CameraHelper.Main());

        QuadAtmosphereMPB = new MaterialPropertyBlock();

        SetupGenerationConstants();
    }

    protected override void Start()
    {
        base.Start();

        if (Cache == null)
            if (gameObject.GetComponentInChildren<QuadStorage>() != null)
                Cache = gameObject.GetComponentInChildren<QuadStorage>();

        if (tccps == null)
            if (gameObject.GetComponentInChildren<TCCommonParametersSetter>() != null)
                tccps = gameObject.GetComponentInChildren<TCCommonParametersSetter>();

        if (NPS != null)
            NPS.LoadAndInit();

        if (PrototypeMesh == null)
            SetupMesh();

        if (Atmosphere != null)
        {
            Atmosphere.OnBaked += OnAtmosphereBaked;
            Atmosphere.InitPlanetoidUniforms(this);
        }

        if (Cloudsphere != null)
        {
            Cloudsphere.InitUniforms();
        }

        ReSetupQuads(); //NOTE : Force resetup on start.
    }

    protected override void Update()
    {
        base.Update();

        CheckCutoff();

        if (LODTarget != null)
            DistanceToLODTarget = Vector3.Distance(transform.position, LODTarget.position);
        else
            DistanceToLODTarget = -1.0f;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            DrawNormals = !DrawNormals;
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (Atmosphere != null) Atmosphere.TryBake();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            if (Atmosphere != null)
            {
                Atmosphere.ReanimateAtmosphereUniforms(Atmosphere, this);
            }
        }

        if (CameraHelper.Main() != null)
            FrustumPlanes = GeometryUtility.CalculateFrustumPlanes(CameraHelper.Main());

        if (Atmosphere != null) Atmosphere.SetUniforms(QuadAtmosphereMPB, null, false, true);

        if (!ExternalRendering)
        {
            Render();
        }

        CheckLOD();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Atmosphere != null)
            Atmosphere.OnBaked -= OnAtmosphereBaked;
    }

    protected override void OnRenderObject()
    {
        base.OnRenderObject();
    }

    protected override void OnApplicationFocus(bool focusStatus)
    {
        base.OnApplicationFocus(focusStatus);

        if (focusStatus == true)
        {
            //NOTE : So, when unity recompiles shaders or scripts from editor 
            //while playing - quads not draws properly. 
            //1) Reanimation of uniforms/mpb can't help.
            //2) MaterialPropertyBlock.Clear() in Reanimation can't help.
            //3) mpb = null; in Reanimation can't help.
            //4) All parameters are ok in mpb.
            //5) Problem not in MainRenderer.
            //I think i've lost something...
            //This ussue take effect only with mpb, so dirty fix is:
            //ReSetupQuads();
            //NOTE : Fixed. Buffers setted 1 time. Need to update when focus losted.

            ReanimateQuadsBuffers(false);

            if (Atmosphere != null)
            {
                Atmosphere.ReanimateAtmosphereUniforms(Atmosphere, this);
            }
        }
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
    }

    private void PlanetOnAtmosphereChanged(Planet p)
    {
        for (int i = 0; i < Quads.Count; i++)
        {
            Quads[i].QuadMaterial.shaderKeywords = p.GetKeywords().ToArray();
        }

        Debug.Log("Planetoid: PlanetOnAtmosphereChanged");
    }

    private void OnAtmosphereBaked(Atmosphere a)
    {
        if (a != null)
        {
            a.ReanimateAtmosphereUniforms(a, this);
        }
    }

    public void ReanimateQuadsBuffers(bool resetup = false)
    {
        if (resetup) { ReSetupQuads(); return; }

        if (Quads != null)
        {
            if (Quads.Count != 0)
            {
                for (int i = 0; i < Quads.Count; i++)
                {
                    Quads[i].Uniformed = false;
                }
            }
        }
    }

    private void SetupGenerationConstants()
    {
        GenerationConstants = PlanetGenerationConstants.Init(PlanetRadius, TerrainMaxHeight);
    }

    public void CheckLOD()
    {
        if (Time.time > LastLODUpdateTime + LODUpdateInterval && UseLOD)
        {
            LastLODUpdateTime = Time.time;

            for (int i = 0; i < Quads.Count; i++)
            {
                Quads[i].CheckLOD();
            }
        }
    }

    public void Render(Camera camera)
    {
        if (Quads != null)
        {
            if (PlanetQuadsEnabled)
            {
                for (int i = 0; i < Quads.Count; i++)
                {
                    if (Quads[i] != null)
                        Quads[i].Render(camera, DrawLayer);
                }
            }
        }

        if (Atmosphere != null)
        {
            if (AtmosphereEnabled)
            {
                Atmosphere.Render(camera, Origin, DrawLayer);
            }
        }

        if (Cloudsphere != null)
        {
            if (CloudsphereEnabled)
            {
                Cloudsphere.Render(camera, DrawLayer);
            }
        }
    }

    public void Render()
    {
        Render(CameraHelper.Main());
    }

    public void CheckCutoff()
    {
        //Prevent fast jumping of lod distances check and working state.
        if (Vector3.Distance(LODTarget.transform.position, transform.position) > PlanetRadius * 2 + LODDistances[0])
        {
            for (int i = 0; i < Quads.Count; i++)
            {
                Quads[i].StopAllCoroutines();
            }

            Working = false;
        }
    }

    [ContextMenu("DestroyQuads")]
    public void DestroyQuads()
    {
        for (int i = 0; i < Quads.Count; i++)
        {
            if (Quads[i] != null)
                DestroyImmediate(Quads[i].gameObject);
        }

        Quads.Clear();
        MainQuads.Clear();

        if (QuadsRoot != null) DestroyImmediate(QuadsRoot);

        if (PrototypeMesh != null) DestroyImmediate(PrototypeMesh);
    }

    [ContextMenu("SetupMesh")]
    public void SetupMesh()
    {
        if (PrototypeMesh != null) DestroyImmediate(PrototypeMesh);

        PrototypeMesh = MeshFactory.SetupQuadMesh();
    }

    [ContextMenu("UpdateLODDistances")]
    public void UpdateLODDistances()
    {
        LODDistances = new float[LODMaxLevel + 1];

        for (int i = 0; i < LODDistances.Length; i++)
        {
            if (i == 0)
                LODDistances[i] = PlanetRadius;
            else
            {
                LODDistances[i] = LODDistances[i - 1] / LODDistanceMultiplierPerLevel;
            }
        }
    }

    public float GetLODOctaveModifier(int LODLevel, bool invert = false)
    {
        if (OctaveFade)
        {
            int id = invert ?
                (LODDistances.Length / (LODLevel + 2 + ((LODDistances.Length - LODOctaves.Length) / LODOctaves.Length))) :
                LODOctaves.Length - (LODDistances.Length / (LODLevel + 2 + ((LODDistances.Length - LODOctaves.Length) / LODOctaves.Length)));

            id -= 1;

            if (LODOctaves != null && LODOctaves.Length > 1 && !(id > LODOctaves.Length))
            {
                return LODOctaves[id];
            }
            else
                return 1.0f;
        }
        else
        {
            return 1.0f;
        }
    }

    public int GetCulledQuadsCount()
    {
        return Quads.Count(x => !x.Visible);
    }

    public Quad GetMainQuad(QuadPosition position)
    {
        foreach (Quad q in MainQuads)
        {
            if (q.Position == position)
                return q;
        }

        return null;
    }

    public Mesh GetMesh(QuadPosition position)
    {
        return PrototypeMesh;
    }

    [ContextMenu("SetupQuads")]
    public void SetupQuads()
    {
        SetupGenerationConstants();

        if (Quads.Count > 0)
            return;

        if (tccps == null)
            if (gameObject.GetComponentInChildren<TCCommonParametersSetter>() != null)
                tccps = gameObject.GetComponentInChildren<TCCommonParametersSetter>();

        SetupMesh();
        SetupRoot();

        SetupMainQuad(QuadPosition.Top);
        SetupMainQuad(QuadPosition.Bottom);
        SetupMainQuad(QuadPosition.Left);
        SetupMainQuad(QuadPosition.Right);
        SetupMainQuad(QuadPosition.Front);
        SetupMainQuad(QuadPosition.Back);

        UpdateLODDistances();

        if (NPS != null)
            NPS.LoadAndInit();
    }

    [ContextMenu("ReSetupQuads")]
    public void ReSetupQuads()
    {
        DestroyQuads();
        SetupQuads();
    }

    public void SetupRoot()
    {
        if (QuadsRoot == null)
        {
            QuadsRoot = new GameObject("Quads_Root");
            QuadsRoot.transform.position = transform.position;
            QuadsRoot.transform.rotation = transform.rotation;
            QuadsRoot.transform.parent = transform;
        }
        else
        {
            return;
        }
    }

    public void SetupMainQuad(QuadPosition quadPosition)
    {
        GameObject go = new GameObject("Quad" + "_" + quadPosition.ToString());
        go.transform.parent = QuadsRoot.transform;
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        Mesh mesh = GetMesh(quadPosition);
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(PlanetRadius * 2, PlanetRadius * 2, PlanetRadius * 2));

        Material material = MaterialHelper.CreateTemp(ColorShader, "Quad");

        Quad quadComponent = go.AddComponent<Quad>();
        quadComponent.CoreShader = CoreShader;
        quadComponent.Planetoid = this;
        quadComponent.QuadMesh = mesh;
        quadComponent.QuadMaterial = material;
        quadComponent.SetupEvents(quadComponent);

        if (Atmosphere != null) Atmosphere.InitUniforms(null, quadComponent.QuadMaterial, false);

        QuadGenerationConstants gc = QuadGenerationConstants.Init(TerrainMaxHeight);
        gc.planetRadius = PlanetRadius;

        gc.cubeFaceEastDirection = quadComponent.GetCubeFaceEastDirection(quadPosition);
        gc.cubeFaceNorthDirection = quadComponent.GetCubeFaceNorthDirection(quadPosition);
        gc.patchCubeCenter = quadComponent.GetPatchCubeCenter(quadPosition);

        quadComponent.Position = quadPosition;
        quadComponent.ID = QuadID.One;
        quadComponent.generationConstants = gc;
        quadComponent.SetupCorners(quadPosition);
        quadComponent.ShouldDraw = true;
        quadComponent.ReadyForDispatch = true;

        Quads.Add(quadComponent);
        MainQuads.Add(quadComponent);
    }

    public Quad SetupSubQuad(QuadPosition quadPosition)
    {
        GameObject go = new GameObject("Quad" + "_" + quadPosition.ToString());

        Mesh mesh = GetMesh(quadPosition);
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(PlanetRadius * 2, PlanetRadius * 2, PlanetRadius * 2));

        Material material = MaterialHelper.CreateTemp(ColorShader, "Quad");

        Quad quadComponent = go.AddComponent<Quad>();
        quadComponent.CoreShader = CoreShader;
        quadComponent.Planetoid = this;
        quadComponent.QuadMesh = mesh;
        quadComponent.QuadMaterial = material;
        quadComponent.SetupEvents(quadComponent);
        quadComponent.SetupCorners(quadPosition);

        if (Atmosphere != null) Atmosphere.InitUniforms(null, quadComponent.QuadMaterial, false);

        QuadGenerationConstants gc = QuadGenerationConstants.Init(TerrainMaxHeight);
        gc.planetRadius = PlanetRadius;

        quadComponent.Position = quadPosition;
        quadComponent.generationConstants = gc;
        quadComponent.ShouldDraw = false;

        if (qdtccc == null)
            qdtccc = new QuadDistanceToClosestCornerComparer();

        Quads.Add(quadComponent);
        Quads.Sort(qdtccc);

        return quadComponent;
    }
}