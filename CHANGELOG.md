# StarMessenger

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
