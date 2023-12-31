# RoboSubSim
This is a WIP project aimed at simulating underwater environments for autonomous underwater vehicles (particularly Seawolf 8) targeted at the RoboSub Competitions. There are a lot of features yet to be implemented or even decided, the plan is to get it to work before RoboSub 2024.

### Targets
* Photorealism (HDRP Water, Domain Randomizations, Successful Sim2Real Transfer)
* Robot Simulation (Fake robot that emulates real robot, ie. is controlled by the same code and has the same sort of interaction with the environment)
* Conveniences (Auto labeled bounding boxes, segmentation/depth imaging, data generation, maybe even RL)

### Usage
* Download Unity Hub and get a free license
* Add this project to Unity Hub
* When launching it will ask you to get editor version `2023.1.8f1`, you need to install this version (higher might work)
* The first launch will take a while, on launch go to `Assets/Scenes` and load the `StartScene` for editor mode.
* It should be good to go now. 
* Tested with Windows 11 and Ubuntu22.04. The editor mode works in Ubuntu, but the build version will crash after resetting the scene several times (last tested)

### Note
If you need a working data generation that is capable of generating synthetic data that works in the real world, please see the past repo that uses URP at [https://github.com/XingjianL/Robosub_Unity_Sim](https://github.com/XingjianL/Robosub_Unity_Sim)

### Development Progress Videos (Note: 11/7/2023, TCP control is under restructuring w.r.t [this repo](https://github.com/ncsurobotics/SW8S-Rust) so what's shown in the video does not work currently)
* [multi-robot demo](https://www.youtube.com/watch?v=bfAbpL1laIY)
* [force visualization](https://www.youtube.com/watch?v=ErsCroTt8VM)
* [tcp communication 1](https://youtu.be/co7QoD9fjzU)
* [tcp communication 2](https://youtu.be/8Z5JBNiAVlg)

### Images
![](./readme_images/0.png) 
![](./readme_images/step0.front_cam.instancesegmentation_0.png)
