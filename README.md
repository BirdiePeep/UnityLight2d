DESCRIPTION

This project is used for 2D Lighting for Unity using the LWRP.
Lights are drawn as an additive layer, so it can affect anything previously drawn on screen.
Shadowing is achieved with a 1D polar coordinate technique which allows for very fast dynamic shadows.
The shadowing technique was adopted from this article.
https://www.gamasutra.com/blogs/RobWare/20180226/313491/Fast_2D_shadows_in_Unity_using_1D_shadow_mapping.php

FEATURES

- Easy 2D Lighting
- Shadow Mapping
- Normal Mapping
- On/Off Self Shadowing

INSTALL

- Open your Unity project.
- Using the Unity Package Manager, download and install the "Lightweight RP".
- Create Pipeline Asset using the menu Asset > Create > Rendering > Universal Render Pipeline > Pipeline Asset (Forward Renderer)
- From the Project Settings > Graphics, set the "Scriptable Render Pipeline Settings" field to this new asset.
- Copy this project directory anywhere into your assets.
- Select the "UniveralRenderPipelineAsset_Renderer" that was created earlier.
- From the inspector, click the + symbol to add a new feature and select "Light2d Feature".
- (Optional) Copy or merge the Gizmos folder into the root Assets directory.  This will add small icons in the editor for light sources.
- Everything is setup, now you are ready to use the package.

HOW TO USE

To create a light, right click under the scene hierarchy and use Bird > Point Light option.

To add a shadow caster, add the "LightCollider" script to any object with a Collider2D component.

To use normal maps, use a copy of the "Sprite Lit" material.  Or create a material with the Bird/Light2D/SpriteLit shader.

