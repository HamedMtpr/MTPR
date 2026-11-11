# About This Project
This document describes the four main components of the system architecture and the role each component plays.

- [Back to index](index.md)

## Introduction
This project aims to build a flexible, configuration-driven no-code backend generator that instantly creates backend systems for any application domain with custom business rules—without traditional coding.

It eliminates boilerplate development, adapts to diverse industry requirements, and significantly accelerates software delivery.

## 1. Definition Engine

The Definition Engine is where users describe the system’s data models, relationships, rules, and roles. These definitions can be provided in a configuration format such as JSON or YAML, or through a graphical user interface.

It provides a central place to define how the system’s data is organized and which rules and permissions apply.

## 2. Metadata-Driven Execution Core

The Metadata-Driven Execution Core is the runtime component that reads the definitions and configuration created by users. It uses this metadata to execute system logic dynamically, rather than relying on hardcoded behavior.

This allows the system to adapt its behavior when its definitions or configuration change.

## 3. Dynamic Data Layer

The Dynamic Data Layer manages data structures and database operations based on the system’s definitions. Depending on the chosen database or a custom Object-Relational Mapping (ORM) layer, it can manage table structures, indexes, and queries dynamically.

This layer connects the metadata-driven logic to the underlying data store.

## 4. Business Rule & Workflow Engine

The Business Rule & Workflow Engine handles business logic and process automation. It supports conditional logic, validation, triggers, and asynchronous processes.

It helps the system respond to events, enforce business requirements, and run processes that do not need to complete immediately.

## How the Components Work Together

Users create models, relationships, rules, and roles in the Definition Engine. The Metadata-Driven Execution Core reads these definitions and determines how the system should behave. The Dynamic Data Layer applies the definitions to data storage and database operations, while the Business Rule & Workflow Engine evaluates business conditions and manages automated processes.

