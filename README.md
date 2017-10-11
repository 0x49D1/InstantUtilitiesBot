[![Build Status](https://travis-ci.org/0x49D1/InstantUtilitiesBot.svg?branch=master)](https://travis-ci.org/0x49D1/InstantUtilitiesBot)
# InstantUtilitiesBot
Instant utilities bot for Telegram. For example it uses duckduckgo instant api.    
Find the bot in [telegram](https://telegram.org/): @instant_utilities_bot
### Usage:       
Default command is /ddg, so without any specified commands it will try to use duckduck instant.       
/help - Shows all the commands with examples for some of them       
**With use of external services:**  
/ddg - Instant answers from duckduckgo.com. Example: /ddg 15km to miles       
/ip - Information about selected ip address (location, etc). But feel free to mess IP with texts or send LIST of IPs. Example /ip xxx.xxx.xxx.xxx     
/blockchain - Gets link to check bitcoin address/transaction information on blockchain.info
/hash - Calculate hash. Use like this: /hash sha256 test     
/guid - Generate Global Unique Identifier        
**Internal commands:**       
/tounixtime - Convert datetime to unixtimestamp. Message must be like in format: dd.MM.yyyy HH:mm:sss 01.09.1980 06:32:32. Or just text 'now'       
/fromunixtime - Converts back from epoch timestamp to datetime string.     
/toepoch - Calculate unix timestamp for date in format dd.MM.yyyy HH:mm:ss       
/hash - Calculate hash. Use like this: /hash sha256 test       
/formatjson - Reformats provided JSON string into pretty idented string     
/tobase64 - Encode to base64     
/frombase64 - Decode from base64     
/urlencode - URL-encodes a string and returns the encoded string.    
/urldecode - Decodes URL-encoded string   
/strlen - Returns length of the provided string
