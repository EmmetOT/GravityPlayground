# GravityPlayground (aka Ricercar)

This is a prototype of a 2D platformer idea I had, with some cool shader magic.

Ricercar (which is an Italian term for a kind of musical composition, meaning “to search out”) was an experiment in gravity and particles. I wanted to learn about making a 2D character controller, and also experiment with compute shaders. This was the result.

There are a few cool things about Ricercar: the first is that any sprite can be converted into “gravity map” which is sort of like a baked lightmap but for precomputed gravity calculations. The second is a hand-implemented particle system which makes use of this map to spawn massive numbers of sprites, to create effects like smoke or fire. Finally, there’s a fun character controller with a little gun, wheeeee!!

[GRAVITYMAP](https://user-images.githubusercontent.com/18707147/187958323-b143d277-1a5b-4fe9-9428-92c97fff30bf.gif)

Convert any sprite into a "gravity map" which encodes a 2D vector towards the "mass" of the sprite. This allows for weird gravitational surfaces which would be too expensive to compute at runtime.

![GRAVITYGIF](https://user-images.githubusercontent.com/18707147/187958343-63b40d92-770c-422b-a77f-00155d84735a.gif)

Maps can easily be sampled, both on the CPU and GPU side. The gravity is even extrapolated outside the edge of the map.

![ezgif com-gif-maker](https://user-images.githubusercontent.com/18707147/187958904-88e0c9f7-d9f4-4a01-afb9-068cf8fd9428.gif)

You can create some pretty cool visuals with this. In this gif there are up to 200,000 particles with no lag. All the gravity sources and particle sources can be modified and moved around live, it's very fun.

![gravity2](https://user-images.githubusercontent.com/18707147/187958999-790a647b-a612-4903-b1ca-40058fdd3a26.gif)

Also very fun to just jump around in.

![gravity1](https://user-images.githubusercontent.com/18707147/187959090-1b91eadf-b96d-482b-b21c-1238c2f0ee13.gif)

This project ended up spawning two others which are also here on my GitHub - an SDF based line drawing tool [(LineDrawer)](https://github.com/EmmetOT/LineDrawer) and a tool for sorting large arrays on the GPU. [(BufferSorter)](https://github.com/EmmetOT/BufferSorter)

![gravity3](https://user-images.githubusercontent.com/18707147/187959276-439481cd-909a-46f8-81dc-89b76ba42a53.gif)

The particles can either collide and disappear or bounce with rudimentary physics.
