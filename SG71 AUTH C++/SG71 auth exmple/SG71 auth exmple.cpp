// SG71 auth exmple.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>

#include "SG71auth.h"
#include <iostream>
#include <string>

int main() {
    std::cout << "SG71 Auth System - C++ Client Demo" << std::endl;
    std::cout << "-----------------------------------" << std::endl;

    std::string adminId;
    std::string appName;
    std::string appVersion = "1.0";
    std::cout << "Admin ID: ";


    // Initialize Client
    SG71::SG71Client client("gTrgRkQAsFUazbxXC7Gt4pBJtP33", "test", appVersion);

    // 1. Initialize
    std::cout << "\nInitializing..." << std::endl;
    SG71::ApiResponse initResult = client.Initialize();
    if (!initResult.success) {
        std::cout << "Initialization Failed: " << initResult.message << std::endl;
        return 1;
    }
    std::cout << "Initialization Success: " << initResult.message << std::endl;

    while (true) {
        std::cout << "\nChoose an action:" << std::endl;
        std::cout << "1. Login" << std::endl;
        std::cout << "2. Register" << std::endl;
        std::cout << "3. Exit" << std::endl;
        std::cout << "Selection: ";

        int choice;
        std::cin >> choice;
        std::cin.ignore(); // Clear newline

        if (choice == 1) {
            std::string username, password;
            std::cout << "\nUsername: ";
            std::getline(std::cin, username);
            std::cout << "Password: ";
            std::getline(std::cin, password);

            std::cout << "Logging in..." << std::endl;
            SG71::ApiResponse result = client.Login(username, password);

            if (result.success) {
                std::cout << "Login Successful!" << std::endl;
                std::cout << "Message: " << result.message << std::endl;
                if (SG71::SG71Client::isLoggedIn) {
                    std::cout << "User: " << SG71::SG71Client::currentUser.username << std::endl;
                    std::cout << "Expires: " << SG71::SG71Client::currentUser.expires << std::endl;
                    std::cout << "HWID: " << SG71::SG71Client::currentUser.hwid << std::endl;
                }
            }
            else {
                std::cout << "Login Failed: " << result.message << std::endl;
            }
        }
        else if (choice == 2) {
            std::string username, password, license;
            std::cout << "\nUsername: ";
            std::getline(std::cin, username);
            std::cout << "Password: ";
            std::getline(std::cin, password);
            std::cout << "License Key: ";
            std::getline(std::cin, license);

            std::cout << "Registering..." << std::endl;
            SG71::ApiResponse result = client.Register(username, password, license);

            if (result.success) {
                std::cout << "Registration Successful: " << result.message << std::endl;
            }
            else {
                std::cout << "Registration Failed: " << result.message << std::endl;
            }
        }
        else if (choice == 3) {
            break;
        }
        else {
            std::cout << "Invalid selection." << std::endl;
        }
    }

    return 0;
}


