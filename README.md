# RoboSubSim
This is a maintained project aimed at simulating underwater environments for autonomous underwater vehicles (particularly Seawolf 8) targeted at the RoboSub Competitions. There are a lot of features yet to be implemented or even decided, the plan is to get it to work before RoboSub 2024.

Here is the paper (for a course project) for various features and architecture in the simulator: [https://drive.google.com/file/d/1eip_NG7Cf2Fsl2w5lEbMnAuSLVgT55id/view?usp=sharing](https://drive.google.com/file/d/1eip_NG7Cf2Fsl2w5lEbMnAuSLVgT55id/view?usp=sharing)



### Targets
* Photorealism (HDRP Water, Domain Randomizations, Successful Sim2Real Transfer)
* Robot Simulation (Fake robot that emulates real robot, ie. is controlled by the same code and has the same sort of interaction with the environment)
* Conveniences (Auto labeled bounding boxes, segmentation/depth imaging, data generation, maybe even RL)


### Usage (Packaged Application)
Go to Releases

### Usage (With Unity Editor)
* Download Unity Hub and get a free license
* Add this project to Unity Hub
* When launching it will ask you to get editor version `2023.1.8f1`, you need to install this version (higher might work)
* The first launch will take a while, on launch go to `Assets/Scenes` and load the `StartScene` for editor mode.
* It should be good to go now. 

### Development Progress Videos (Note: 2024/04/21)
Dynamics Testing: [https://youtu.be/PzeR5JaaQD4](https://youtu.be/PzeR5JaaQD4)\
Trajectory Visualization: [https://youtu.be/6XFExcnqDvA](https://youtu.be/6XFExcnqDvA)\
Synthetic Image Generation w/ Domain Randomization: [https://youtu.be/zcOHj0vwbpA](https://youtu.be/zcOHj0vwbpA)


### Images 
#### (Top: Synthetic and Real Images, Bottom: Detection from YOLOv5n Trained on only Synthetic Images)
![](./readme_images/real_and_synthetic.png) 
![](./readme_images/real_and_synthetic_inf.png)
