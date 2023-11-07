# Indirect Rendering System

GPU instancing tool for Unity's Universal Rendering Pipeline (URP).

### Table of Contents
- [Introduction](#introduction)
- [Features](#features)
- [Installation](#installation)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [License](#license)


### Introduction

The Indirect Rendering System is a GPU Instancing tool for Unity's Universal Rendering Pipeline that is capable of efficiently rendering a large number of static meshes through Graphics.RenderMeshIndirect API.

The system uses Compute Shaders to sort meshes based on the distance from the camera, calculating the appropriate Level of Detail that should be rendered. 
After that, culling is performed based on camera view frustum and a hierarchical depth buffer, thus excluding occluded or outside of view meshes from rendering.

This package is a reimplementation of Elvar Orn Unnthorsson's technique that was presented here:
https://github.com/ellioman/Indirect-Rendering-With-Compute-Shaders


### Features

- Static meshes GPU Instancing
- Level of Details
- Distance Culling
- Frustum Culling
- Hi-Z Occlusion Culling
- Detail Culling

### Installation

Install via UPM package available in [Indirect Rendering System](https://github.com/mihairobertcr/IndirectRenderingSystem.git) page.

![package-manager](https://github.com/mihairobertcr/IndirectRenderer/assets/77929450/95d925a6-5674-49f9-9c90-136bd41b2df8)

![package-url](https://github.com/mihairobertcr/IndirectRenderer/assets/77929450/9291b0c9-15ec-490a-8661-eb6cb00103b0)

Or add `"com.keensight.indirect-rendering": "https://github.com/mihairobertcr/IndirectRenderingSystem.git"` to `Packages/manifest.json`.


### Getting Started

- TODO

### Configuration

- TODO

### License

Copyright (C) 2023 - 2024 Robert-Mihai Crăciun

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
