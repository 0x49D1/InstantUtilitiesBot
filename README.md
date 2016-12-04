[![Build Status](https://travis-ci.org/0x49D1/InstantUtilitiesBot.svg?branch=master)](https://travis-ci.org/0x49D1/InstantUtilitiesBot)
# InstantUtilitiesBot
Instant utilities bot for Telegram. For example it uses duckduckgo instant api.  
### Usage:  
Default command is /ddg, so without any specified commands it will try to use duckduck instant.  
/help - Shows all the commands with examples for some of them  
**With use of external services:**  
/ddg - Instant answers from duckduckgo.com. Example: /ddg 15km to miles  
/ip - Information about selected ip address (location, etc)  
**Internal commands:**  
/tounixtime - Convert datetime to unixtimestamp. Message must be like in format: dd.MM.yyyy HH:mm:sss 01.09.1980 06:32:32. Or just text 'now'  
/toepoch - Calculate unix timestamp for date in format dd.MM.yyyy HH:mm:ss  
/hash - Calculate hash. Use like this: /hash sha256 test  
