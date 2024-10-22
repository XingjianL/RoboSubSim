# Commands for Unity Simulation

## Startups

### **\<SIMCBTOGU\>\<connect\>**

**SIMCBTOGU** | string | simCB connection |\
**connect** | uint8 | toggle |
* non-zeros: attempt simCB connection
* 0: stop simCB connection
---
### **\<SETENVU\>\<select\>**

**SETENVU** | string | change unity scene |\
**select** | uint8 | world selections |
* 0: lobby (startup)
* 1: outdoor pool
* 2: outdoor pool 
* 3: Rectangular Pool
* 4: outdoor pool (8 robots, no cameras)

## Actions
### **\<CAPTUREU\>\<mode\>**

**CAPTUREU** | string | capture and save images |\
**mode** | uint8 | camera capture mode |
* 0: front RGB
* 1: down RGB
* 2: front & down RGB
* 3: front & down Perception
* 4: front & down, RGB and Perception\

Perception: Unity Perception Package to generate labels

---

## Configurations
### **\<CAMCFGU\>\<height\>\<width\>\<exposure\>\<blur\>\<mode\>\<cfg1\>**
**CAMCFGU** | string | configure camera |\
**height** | int32 | image height pixels |\
**width** | int32 | image width pixels |\
**exposure** |float32| exposure time |\
**blur** |float32| blur intensity|\
**mode** | uint8 | camera capture mode (see **CAPTUREU**) |\
**cfg1** | byte | bit-level feature toggles |
* 0b0000_0001: fisheye effect (lens distortion)
* 0b0000_0010: fog
* 0b0000_0100: chromatic aberration
* 0b0000_1000: film grain
* 0b0001_0000: bloom
* 0b0010_0000: lens flare
* 0b0100_0000: show camera view as GUI
* 0b1000_0000: resize simulator window (for RGB images saved by Perception)
---


### **\<ROBCFGU\>\<mass\>\<volume\>\<ldrag\>\<adrag\>\<f_KGF\>\<r_KGF\>**
**CAMCFGU** | string | configure robot |\
**mass** | float32 | mass (kg) | negative values -> sim default |\
**volume** | float32 | water volume displaced (L) | sim default: negative values |\
**ldrag** | float32 | linear movement drag | sim default: negative values |\
**adrag** | float32 | angular movement drag | sim default: negative values |\
**f_KGF** | float32 | forward max thrust (KGF/motor) | sim default: negative values |\
**r_KGF** | float32 | reverse max thrust (KGF/motor) | sim default: negative values |

---

### **\<SCECFGU\>\<cfg1\>\<colorBias\>**
**SCECFGU** | string | configure scene |\
**cfg1** | byte | bit level toggles |
* 0b0000_0001: random water color
* 0b0000_0010: enable physics
* 0b0000_0100: random pool material (appearance)
* 0b0000_1000: random caustic size
* 0b0001_0000: higher pool visibility

**colorBias** | int16 | water color hue bias. Hue: [0,360] |\
**cfg2** | float32 | visibility multiplier |\

---

### **\<ROBINIU\>\<x\>\<y\>\<z\>\<xr\>\<yr\>\<zr\>**
**ROBINIU** | string | set robot initial pose w.r.t. control board documentation |\
**x** | float32 | translation, meters |\
**y** | float32 |\
**z** | float32 |\
**xr** | float32 | rotation, degrees |\
**yr** | float32 |\
**zr** | float32 |

---

### **\<ROBRINU\>\<x\>\<y\>\<z\>\<xr\>\<yr\>\<zr\>**
**ROBRINU** | string | set **Random** robot initial pose within bounds w.r.t. control board documentation |\
**x** | float32 | translation bounds, meters |\
**y** | float32 |\
**z** | float32 | bounded to below water level |\
**xr** | float32 | rotation bounds, degrees |\
**yr** | float32 |\
**zr** | float32 |

---

### **\<RESTARTU\>\<x\>**
**RESTARTU** | string | close TCP, close simCB, restart TCP after 4 seconds (does not restart simcb). The messaging sometimes gets stuck so this is recommended to be sent at the end of simulated run from Rust |\
**x** | any | not used | 