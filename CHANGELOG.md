# StarMessenger

## 1.1.0.0 - 2026-11-04
* Changed: StarMessages are sent decoupled from the sequence. Triggers and instructions only queue the message,
  the sequence continues immediately. The message is sent as soon as N.I.N.A. has saved the image.
* Added: Option 'Wait for delivery' for the StarMessage instructions (default: off)
* Added: Errors while sending are shown as N.I.N.A. notification
* Fix: "StarMessage - no new image in history found" when saving the image took longer than 15 s
  (e.g. when not dithering after every exposure)
* Fix: High CPU load while waiting for the image to be saved
* Fix: Ntfy and Email credentials got lost when changed while N.I.N.A. was running
* Fix: Negative values are allowed in conditions (e.g. Temperature < -10)
* Fix: Disabling one parameter in the options also disabled the parameter selected in a condition
* Fix: Temporary preview images of Email messages were not deleted
* Fix: Sound and priority got lost when a trigger was copied
* Changed: Temperature is taken from the focuser, otherwise from the weather station. Values that are not available
  for an image (e.g. no rotator, no guiding) are left out instead of being shown as 0
* Changed: Messages of an aborted sequence that are still waiting for their image are discarded

## 1.0.1.0 - 2025-10-22
* Fix: Messenger credentials no longer disappear after N.I.N.A update

## 1.0.0.0 - 2025-07-15
* Added: CCL - Consecutive Counter Limit
	  Number of consecutive condition fulfillments until a StarMessage is sent

* 
## 0.3.1.0 - 2024-11-16 (beta)
* Fix: No empty message with 'Dec: RA:'


## 0.3.0.0 - 2024-11-15 (beta)
* Added Instructions:
  - StarMessage via Ntfy (offline messaging (via WiFi) possible )
* Added Triggers:
  - StarMessage via Ntfy after exposures
  - StarMessage via Ntfy by condition
* Feature: Added Median and Target parameter
* Fix: Increased timeout for sending
 

## 0.2.1.0 - 2024-8-8 (beta)
* Fix: follow-up issues in plugin when transfer of images from camera to filesystem is to slow. TimeOuts increased.


## 0.2.0.0 - 2024-8-1 (beta)
* Fix: Bug when sending message but no light image existing
* Fix: Bug when more then one SendMessage trigger configured
* Feature: Condition configuration in sequence savable
* Feature: Priority and notification -sound choosable for pushover messages


## 0.1.2.0 - 2024-7-3 (beta)
* Fix: Bug when using conditions of integer type (Stars, RotatorPosition, FocusPosition) 


## 0.1.1.0 - 2024-6-25 (beta)
* Fix: Sending via Pushover or email is prevented because the images are too large.
* Fix: If no Pushover/email configuration is set, decrypt function is creating unnecessary log entries. 


## 0.1.0.0 - 2024-6-9 (beta)
* Initial release
* Added Instructions:
  - StarMessage via Email
  - StarMessage via to Pushover
* Added Triggers:
  - StarMessage via Email after exposures
  - StarMessage via Pushover after exposures
  - StarMessage via Email by condition
  - StarMessage via Pushover by condition
