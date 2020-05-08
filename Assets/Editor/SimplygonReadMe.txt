Simplygon Unity Plugin 2.5.1
This is a short version of the user guide. For more extensive information please go to www.simplygon.com. 


UNITY 5
If you have been using a previous version of the Simplygon plug-in in Unity 5, the shading networks of the 'Standard' and 'Standard (Specular setup)' will not be up to date. In order to update these shading networks to use the new default channels and nodes, click on the 'Edit Shading Network...' button in the 'Advanced Settings' tab and select the 'Standard' shader in the SN Setup window followed by clicking the 'Load Default' and 'Save' buttons. Repeat this procedure for the 'Standard (Specular setup)' shader. The procedure may be repeated for any shader for which you want to use the latest default channels and nodes. Please note that saving a shader's shading network will overwrite the previous setup.

If you want all shaders to use their respective default channels and nodes, close Unity and delete the files SimplygonShadingNetworkSetup.json and SimplygonShadingNetworkSetup.json.meta located in the project's Assets/Editor folder. Once Unity is restarted and the project reopened the default shading networks will be in use and the SimplygonShadingNetworkSetup.json file will be recreated if any shader's shading network is edited and saved.


HOW TO INSTALL SIMPLYGON
1. Start Unity.
2. Go to asset store and install the Simplygon Plugin.
3. In Unity, click Window -> Simplygon.
4. Log into Simplygon using your preferred account details; either towards Simplygon Grid using the account you've setup locally or Simplygon Cloud using your Simplygon account (as registered on simplygon.com). Please see https://www.youtube.com/watch?v=hCPlLDcXfb0 for details on getting started with Unity and Simplygon Grid.


CREATING YOUR FIRST LOD
Start by configuring the LOD setting in the Create LOD tab in the Simplygon GUI. There are a lot of settings here that can be used to fine-tune the result you get from Simplygon. 

Now you need to select the asset you want to optimize. This can be done ether by selecting an asset inside of your scene or in the project explorer. Notice that the Target Assets section will show the name of the asset(s) you have selected.

Now, click the Simplygon button!

Your request is now being sent to and processed by the Simplygon servers. You can follow the progress of your LOD creation in the Manage Jobs tab of the Simplygon menu. 

You will find your LODs in your project explorer Assets/LODs/*AssetName_Date*.


OTHER FEATURES

SAVING SETTINGS FILES
You can save and load settings files for your processing’s. This feature is meant to speed up your workflow. When you have found a good setting for a LOD that you would like to apply to many assets. Save the setting so you won’t forget it in the future.


2.5.1
Fixes
---------------------------------
- Fixed: Application.data called when constructing objects (previously generating the "get_dataPath is not allowed to be called from a ScriptableObject constructor...").
- Fixed: Ongoing jobs disappears from the Manager Jobs tab on logout/login when processing towards the Simplygon Grid.
- Fixed: Material LOD not processed correctly (material missing in processing result) when initiating processing via the Quick Start tab.

Improvements
---------------------------------
- Added 'Grid' button to login screen - clicking this button sets default login credentials and server address for processing towards Simplygon Grid.
- Added support for up to 4 UV sets, selectable via the Texture Property shading network node.

Changes
---------------------------------
- N/A

Known issues/future improvements
---------------------------------
- No support for the Standard shader 'Source' attribute (as of v5.4.0): source is always baked to SmoothnessTextureChannel 0 (specular/metallic alpha) - manual editing of the shading network is necessary in order to construct a sub-network that handles the _SmoothnessTextureChannel shader property ('Float/Range' property node).
- Shader property _GlossMapScale is not taken into account in the default shading network of the 'Standard' and 'Standard (Specular setup)' shaders - manual editing of the shading network is necessary in order to take _GlossMapScale into account during baking.
- Shading network editor too cluttered - add various features to support user friendlieness, e.g. zoom, collapse and grouping of nodes and functionality.
- Shading network targeted shader offers only 1 target template - Standard shader is always targeting (baking towards) the 'Standard (Specular setup) shader' - manual editing of the 'Standard' shader shading network necessary in 
order to get results applicable for the 'Standard' shader instead of the 'Standard (Specular setup)' shader.
- Shader 'Offset' property not supported.

2.5.0
- Now generating normal map for material LOD even if the original asset's shader has no normal map set.
- Corrected various instantiation taking place in calls to OnGUI.
- Now automatically enabling Normals Calculation if either the 'repair invalid normals' or 'replace normals' option is active.
- Fixed shading network texture node resetting UV slot selection.
- Improved visual feedback on asset export, upload, download and import in order to better communicate when enter play mode shall be avoided.
- (Unity 5) Improved handling of stand-in textures - unused Standard shader slots will not generate and set a default texture when generating a material LOD.
- (Unity 5) Now enabling shader keywords on import in order to have the resulting asset's materials rendered as expected, without having to inspect/expand the resulting asset's material.

2.4.0
- Added support for forced asset re-upload via the 'Use Cached Asset' option in the Advanced Settings tab.

2.3.1
- Fixed an issue where incorrect vertex colors were applied to the processing result.
- Fixed LOD Group not being automatically created in Unity 5 Personal.

2.3.0
- Added shading network vertex color node.
- Improved shading network texture re-use in order to decrease upload size.
- Added support for selecting UV on texture properties in the shading network. Current and new nodes will use UV0.
- Fixed uploads failing due to skinned mesh renderers having no bones.
- (Unity 5) Updated standard shaders' shading network to support changed emission properties 
- (Unity 5) Fixed standard shaders' 'UV Set' shader property not being respected by the shading network.
- Material LOD normal map tiling as well as secondary normals will be supported in an upcoming release or backend fix.