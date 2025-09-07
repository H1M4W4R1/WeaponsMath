<div align="center">
  <h1>Weapons Math</h1>
</div>

# About

This is a quick weekend project I did to compute weapon edge types (mostly for white weapons, but works for bullets 
and arrows too). This generates a primitive classification of weapon edges based on their geometry: blunt, spike, 
and blade.

* Blunt - flat surface, usually common in hammers
* Spike - sharp point, usually common in medieval maces and also known as pointy element of sword or arrow tip
* Blade - sharp edge, usually common in axes, swords and many other weapons

# How it works

The algorithm is based on mesh triangles, vertices and normals. When distance vector between current vertex and 
specified neighbour and normal vector dot product (angle) meets desired criteria of parallelism to perpendicularity 
ratio then it's marked as edge of specified type.
It can also factor distance between vertex and neighbours and some other minor modifiers into computation algorithm.

In other short terms: my brain wrote this at 1am on Saturday and I have no f-ing idea why this works so well...
Okay, unless you're using too complex mesh.

# How to use

Simply create a weapon with mesh and call `WeaponEdgeClassifier.ClassifyAllVertices` on that mesh. Provide all 
necessary parameters and you should be fine.

It is heavily recommended to have separate mesh for computation as any decorations (like embossed patterns) will 
generate very non-predictable results and may be marked as blade or spike when it should be blunt. Also: low-poly 
meshes work way better than high-poly ones. It's faster to compute them, and they yield better results.

You can see `WeaponEdgeTypeDrawer.cs` for analysis purposes.

# Support

This comes without support as is, at it was mostly a fun project to check my capabilities of handling 3D geometry...

**USE AT YOUR OWN RISK (REALLY)**