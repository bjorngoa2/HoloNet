# Data Clumps

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Bloaters
Data Clumps
Signs and Symptoms
Sometimes different parts of the code contain identical groups of variables (such as parameters
for connecting to a database). These clumps should be turned into their own classes.
Reasons for the Problem
Often these data groups are due to poor program structure or "copypasta programming”.
If you want to make sure whether or not some data is a data clump, just delete one of the data
values and see whether the other values still make sense. If this is not the case, this is a good
sign that this group of variables should be combined into an object.

 

Treatment
• If repeating data comprises the �elds of a class, use Extract Class to move the �elds to their
own class.
• If the same data clumps are passed in the parameters of methods, use Introduce Parameter
Object to set them off as a class.
• If some of the data is passed to other methods, think about passing the entire data object to
the method instead of just individual �elds. Preserve Whole Object will help with this.
• Look at the code used by these �elds. It may be a good idea to move this code to a data
class.
Payoff
• Improves understanding and organization of code. Operations on particular data are now
gathered in a single place, instead of haphazardly throughout the code.
• Reduces code size.
When to Ignore
Passing an entire object in the parameters of a method, instead of passing just its values
(primitive types), may create an undesirable dependency between the two classes.
Reading is boring
Aren't you bored of reading so much? Try out our new interactive

READ NEXTRETURN
Object-Orientation Abusers  Long Parameter ListDesign Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
learning course on refactoring. It has more content and much more
fun.
 Learn more
