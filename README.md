# Rapid Integration & Development Environment (RIDE)

## Overview

RIDE combines a range of commonly used simulation features in a drag-and-drop development environment, including 3D Geospatial terrain, NPC and vehicle placement, and AI behaviors. Learn more about the capabilities of RIDE and view the API at https://ride.ict.usc.edu.

## Documentation

See the [Getting Started](https://ride.ict.usc.edu/getting-started) on the main RIDE website, as well as this GitHub's associated Wiki.

## Packages

RIDE is game engine independent, with a primary focus on Unity. RIDE is package-based. These packages can be used independently for Unity-based AI development, with native integrations with AWS, Azure, OpenAI, and Stability AI, among others.

The main packages are:

* RIDE.Abstract: contains main RIDE interfaces and definitions
* RIDE.Core: core functionality, including logging, configuration, web service interface, etc. 
* RIDE.Cognition: contains interfaces, implementations, and samples for audio-visual sensing, speech recognition, natural language processing and text-to-speech

## License

The public portion of RIDE is licensed under the [USC-RL v3.0 license](https://github.com/USC-ICT/ride?tab=License-1-ov-file#readme), a permissive license for academic and personal use. For commercial and government purpose use, please [contact us](https://ride.ict.usc.edu/support/contact).

## Citation

When publishing work that uses RIDE, please cite one of the following papers:

```
@inproceedings{hartholt2021rapid,
  title={Rapid prototyping for simulation and training with the Rapid Integration \& Development Environment (RIDE)},
  author={Hartholt, Arno and McCullough, Kyle and Fast, E and Leeds, A and Mozgai, S and Aris, T and Ustun, V and Gordon, AS and McGroarty, C},
  booktitle={Proceedings of the 2021 Interservice/Industry Training, Simulation, and Education Conference (I/ITSEC)},
  year={2021}
}
```