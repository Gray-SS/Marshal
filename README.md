# 🚀 **Marshal Documentation**  

Welcome to the official documentation for **Marshal**! 🎉 Marshal is a compiled programming language built with C# and .NET 8.0, leveraging the LLVM backend to generate machine code. Marshal is an educational project but I'm planning to push it to it's limits! 💻✨

---

## 📖 **Table of Contents**
1. [Getting Started](#getting-started)
2. [Installation](#installation)
3. [Hello World](#hello-world)
4. [Key Features](#key-features)
5. [Language Syntax](#language-syntax)
    - Variables
    - Functions
    - Control Flow
6. [Standard Library](#standard-library)
7. [Compiling Your Code](#compiling-your-code)
8. [Sample Programs](#sample-programs)
9. [Contributing](#contributing)
10. [FAQs](#faqs)

---

## 🛠️ **Getting Started**

Marshal is easy to set up and use. Whether you're an experienced developer or a student eager to learn compiler design, this documentation will help you get started!

### Prerequisites
- ✅ A modern operating system (Linux, macOS, or Windows)
- ✅ .NET SDK 8.0 or newer installed
- ✅ LLVM version 17.0.6 or newer installed

---

## 📦 **Installation**

### Step 1: Clone the Repository
Start by downloading the Marshal project:
```bash
git clone https://github.com/Gray-SS/Marshal.git
```

### Step 2: Build the compiler
Navigate to the project directory and build the compiler:
```bash
cd Marshal/src/Marshal.Compiler
dotnet build
```

### Step 3: Add Marshal to your PATH
(Optional) Add the `marshal` executable to your PATH for easier usage:
```
export PATH=$PATH:/path/to/Marshal/src/Marshal.Compiler/bin/Debug/net8.0
```

---

## 🌟 Hello World

Here’s your first program in Marshal. 🎉  
Create a file named `hello.ms` and add the following code:

```go
import std::io;

func main(): int {
    println("Hello, World!");
    return 0;
}
```

To compile and run the program:

```bash
marshalc -i hello.ms -o hello
./hello
```

Expected output:

```bash
Hello, World!
```

---

## 🔑 Key Features

- **Educational Roots:** Designed to help you learn compiler theory and practice. 📚
- **LLVM-Optimized**: Marshal leverages LLVM for machine code generation. (not optimisation layer at the moment) 🚀  
- **Modern Syntax:** Easy to write and read, even for beginners. ✨  
- **Cross-Platform:** Marshal runs on Windows, macOS, and Linux. 🌐  
- **Extensible:** A great base for adding new features and testing language ideas. 🔧

---

## 📝 Language Syntax
### 📊 Variables

Declare and initialize variables in Marshal:

```go
var age: int = 25;
var name: string = "Alice";
```

### 🧮 Functions

Define reusable functions:

```go
//There is no string concatenation at the moment.
func greet(name: string): void {
    print("Hello, ");
    print(name);
    print("!");
}
```

### 🔄 Control Flow

Control your program's logic with conditions:

```go
//Control the flow of the program with conditions
if (x > 10) {
    println("Greater than 10!");
} else if (x < 10) {
    println("10 or less.");
} else {
    println("10");
}

//for loop going from 0 to 10
for (var i: int = 0; i < 10; i++) {
    println(i);
}

//while loop running until 'i' is greater than 10
var i: int = 0;
while (i < 10) {
    println(i);
    i++;
}
```


### 🌌 Comments

Add single-line comments with //:

```go
// This is a comment
var x: int = 42; // Another comment
```

---

## 🤝 Contributing

Marshal is an open-source project, and we welcome contributions! 🎉 Here's how you can get involved:

- Fork the repository on GitHub.
- Create a new branch for your feature or fix.
- Commit your changes and submit a pull request.

**For major changes, please open an issue first to discuss your ideas.**

## ❓ FAQs
**Q: What platforms does Marshal support?**  
**A:** Marshal supports Windows, macOS, and Linux. You can generate binaries for all these platforms.

**Q: Is Marshal production-ready?**  
**A:** Not yet. Marshal is primarily an educational project, but we encourage experimentation and feedback to push it further.

**Q: Can I extend Marshal with custom features?**  
**A:** Absolutely! Marshal is designed to be extensible. Feel free to add new syntax or functionality and share your improvements.

---

## Thank you for exploring Marshal! ❤️
Let’s push this project to its limits together. Happy coding! 🚀✨