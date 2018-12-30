# DiscordMultiRP

A bot to assist with roleplaying in Discord.

## Features
### Dice roller
The bot looks for dice roll definitions in any message it can see, and responds with the result of
performing that roll. Currently the following commands work:

* (n)d(m) - rolls n m-sided dice. This can be modified with any of the following (must be in this order):
  * !(x) - explodes (include then re-roll) dice greater than or equal to x. If x is omitted, use the maximum value instead.
  * \>(t) - counts dice with value greater than t. If t is omitted, counts the dice with the maximum roll instead.
* (n)df - rolls n fudge dice. A fudge die is a 3-sided die with the face values -1, 0, +1.

All rolls can be modified with +(c) - adds a constant value to the result.

#### Examples
* 1d6 => [4] = 4
* 3d8! => [4] [7] [3] = 14
* 3d8!6 => [8!] [6!] [7!] [1] [7!] [6!] [6!] [8!] [4] [1] = 54
* 2d10> => [9] [3] = 0 successes
* 2d10>5 => [8] [8] = 2 successes
* 2d6!> => [2] [5] = 0 successes
* 2d6!>4 => [6!] [6!] [6!] [1] [5] = 4 successes
* 2d6!4> => [3] [1] = 0 successes
* 2d6!4>5 => [4!] [5!] [3] [3] = 0 successes
* 1d6+3 => [1] + 3 = 4
* 2df => [-] [+] = 0
* 10df+5 => [0] [0] [0] [-] [-] [+] [0] [-] [0] [+] + 5 = 4
