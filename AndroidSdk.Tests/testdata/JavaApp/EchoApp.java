package com.androidsdk;

import java.util.Scanner;

public class EchoApp {
    public static void main(String[] args) {
        // if the args are not empty, print them
        if (args.length > 0) {
            System.out.println("Arguments:");
            for (String arg : args) {
                System.out.println(arg);
            }
            return;
        }

        // Otherwise, echo the input
        Scanner scanner = new Scanner(System.in);
        System.out.println("Enter text to echo. Type 'exit' to quit.");

        while (true) {
            String input = scanner.nextLine();

            if (input.toLowerCase().startsWith("exit")) {
                System.out.println("Exiting...");
                String errorCode = input.substring(4).trim();
                if (!errorCode.isEmpty()) {
                    System.exit(Integer.parseInt(errorCode));
                }
                break;
            }

            if (input.toLowerCase().startsWith("error")) {
                System.err.println("You entered: " + input);
            } else {
                System.out.println("You entered: " + input);
            }
        }

        scanner.close();
    }
}
