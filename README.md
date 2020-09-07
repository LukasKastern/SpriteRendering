# SpriteRendering

SpriteRendering aims to provide a rendering backend for 2d entities inside Unitys ECS.

Instead of focussing on 'maximum performance' this package is oriented at giving the user an easy experience while not constraining him to seemingly abitrary constraints.


## Overview

To [install](https://docs.unity3d.com/Manual/upm-ui-giturl.html) the package through the package manager use this url https://github.com/LukasKastern/SpriteRendering.git 

After installing the package the conversion process takes care of creating 2d entities out of SpriteRenderers.

## Shaders

### Sprite Transform

We pass a per instance vector property called **_SpriteTransform** to the shader which specifies the transform of the instance to draw.

The utility method **ObjectToClipPos2D** inside **SpriteRenderingCG.cginc** can be used to transform a vertex by the SpriteTransform.


### Instanced Properties

SpriteRenderering currently supports float and vector shader properties.

To create a new property add the **RegisterInstancedProperty(shaderKeyword, defaultValue)** attrribute to an IComponentData.

To  give an example SpriteRendering uses **SpriteColor** to create a per instance color value:
``` 
    [RegisterInstancedProperty("_Color", 1, 1, 1, 1)]
    public struct SpriteColor : IComponentData
    {
        public Color Value;
    }
```



## Current Limitations

* Tilemaps and Sprite masks are currently not supported.

* Missing optimization for static entities.

* No URP/HDRP shaders included.

* No Culling
