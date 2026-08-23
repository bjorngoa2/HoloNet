# Data Class

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Dispensables
Data Class
Signs and Symptoms
A data class refers to a class that contains only �elds and crude methods for accessing them
(getters and setters). These are simply containers for data used by other classes. These classes
do not contain any additional functionality and cannot independently operate on the data that
they own.
Reasons for the Problem
It’s a normal thing when a newly created class contains only a few public �elds (and maybe
even a handful of getters/setters). But the true power of objects is that they can contain
behavior types or operations on their data.
Treatment
• If a class contains public �elds, use Encapsulate Field to hide them from direct access and
require that access be performed via getters and setters only.

 

• Use Encapsulate Collection for data stored in collections (such as arrays).
• Review the client code that is used by the class. In it, you may �nd functionality that would
be better located in the data class itself. If this is the case, use Move Method and Extract
Method to migrate this functionality to the data class.
After the class has been �lled with well thought-out methods, you may want to get rid of old
methods for data access that give overly broad access to the class data. For this, Remove
Setting Method and Hide Method may be helpful.
Payoff
• Improves understanding and organization of code. Operations on particular data are now
gathered in a single place, instead of haphazardly throughout the code.
• Helps you to spot duplication of client code.
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.

READ NEXTRETURN
Dead Code  Lazy Class
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
 Learn more
