# Singleton Design Pattern

Source: sourcemaking.com (downloaded copy)

---

 / Design Patterns / Creational patterns
Singleton Design Pattern
Intent
• Ensure a class has only one instance, and provide a global point of access to it.
• Encapsulated "just-in-time initialization" or "initialization on �rst use".
Problem
Application needs one, and only one, instance of an object. Additionally, lazy initialization and
global access are necessary.
Discussion
Make the class of the single instance object responsible for creation, initialization, access, and
enforcement. Declare the instance as a private static data member. Provide a public static
member function that encapsulates all initialization code, and provides access to the instance.
The client calls the accessor function (using the class name and scope resolution operator)
whenever a reference to the single instance is required.
Singleton should be considered only if all three of the following criteria are satis�ed:
• Ownership of the single instance cannot be reasonably assigned
• Lazy initialization is desirable
• Global access is not otherwise provided for
If ownership of the single instance, when and how initialization occurs, and global access are
not issues, Singleton is not suf�ciently interesting.

 

The Singleton pattern can be extended to support access to an application-speci�c number of
instances.
The "static member function accessor" approach will not support subclassing of the Singleton
class. If subclassing is desired, refer to the discussion in the book.
Deleting a Singleton class/instance is a non-trivial design problem. See "T o Kill A Singleton" by
John Vlissides for a discussion.
Structure
Make the class of the single instance responsible for access and "initialization on �rst use". The
single instance is a private static attribute. The accessor function is a public static method.
Example
The Singleton pattern ensures that a class has only one instance and provides a global point of
access to that instance. It is named after the singleton set, which is de�ned to be a set
containing one element. The of�ce of the President of the United States is a Singleton. The
United States Constitution speci�es the means by which a president is elected, limits the term
of of�ce, and de�nes the order of succession. As a result, there can be at most one active
president at any given time. Regardless of the personal identity of the active president, the title,
"The President of the United States" is a global point of access that identi�es the person in the
of�ce.

Check list
1. De�ne a private static  attribute in the "single instance" class.
2. De�ne a public static  accessor function in the class.
3. Do "lazy initialization" (creation on �rst use) in the accessor function.
4. De�ne all constructors to be protected  or private .
5. Clients may only use the accessor function to manipulate the Singleton.
Rules of thumb
• Abstract Factory, Builder, and Prototype can use Singleton in their implementation.
• Facade objects are often Singletons because only one Facade object is required.
• State objects are often Singletons.
• The advantage of Singleton over global variables is that you are absolutely sure of the
number of instances when you use Singleton, and, you can change your mind and manage
any number of instances.
• The Singleton design pattern is one of the most inappropriately used patterns. Singletons
are intended to be used when a class must have exactly one instance, no more, no less.
Designers frequently use Singletons in a misguided attempt to replace global variables. A
Singleton is, for intents and purposes, a global variable. The Singleton does not do away
with the global; it merely renames it.
• When is Singleton unnecessary? Short answer: most of the time. Long answer: when it's
simpler to pass an object resource as a reference to the objects that need it, rather than
letting objects access the resource globally. The real problem with Singletons is that they
give you such a good excuse not to think carefully about the appropriate visibility of an
object. Finding the right balance of exposure and protection for an object is critical for

maintaining �exibility.
• Our group had a bad habit of using global data, so I did a study group on Singleton. The next
thing I know Singletons appeared everywhere and none of the problems related to global
data went away. The answer to the global data question is not, "Make it a Singleton." The
answer is, "Why in the hell are you using global data?" Changing the name doesn't change
the problem. In fact, it may make it worse because it gives you the opportunity to say, "Well
I'm not doing that, I'm doing this" – even though this and that are the same thing.
Code examples
JavaSingleton in Java Singleton in Java
C++Singleton in C++: Before and afterSingleton in C++
PHPSingleton in PHP
DelphiSingleton in Delphi
PythonSingleton in Python
READ NEXTRETURN
Structural patterns  Prototype
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
Support our free website and own the eBook!
• 22 design patterns and 8 principles explained in depth
• 406 well-structured, easy to read, jargon-free pages
• 228 clear and helpful illustrations and diagrams
• An archive with code examples in 4 languages
• All devices supported: EPUB/MOBI/PDF formats
 Learn more...

© 2007-2026 SourceMaking.com T erms / Privacy policy
All rights reserved.
