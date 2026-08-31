package net.pollyspeople.storagelabels.feature.legal

/**
 * The privacy notice and licence terms, carried across verbatim from the web app's legal
 * pages so both clients say exactly the same thing. Wording changes belong upstream in
 * storage-labels-ui first; this file is generated from those components.
 */
data class LegalDocument(
    val title: String,
    val sections: List<LegalSection>,
)

data class LegalSection(
    val heading: String,
    val blocks: List<LegalBlock>,
)

sealed interface LegalBlock {
    data class Paragraph(val text: String) : LegalBlock
    data class Bullet(val text: String) : LegalBlock
}

val PrivacyNotice = LegalDocument(
    title = "Privacy Notice for Storage Labels",
    sections = listOf(
        LegalSection(
            heading = "Important: Self-Hosted Software",
            blocks = listOf(
                LegalBlock.Paragraph("Storage Labels is self-hosted software that you install and operate on your own infrastructure. The software developers do not collect, store, or have access to any of your data."),
                LegalBlock.Paragraph("This notice describes how the software handles data locally in your installation. As the operator, you are the data controller and are responsible for compliance with applicable privacy laws."),
            ),
        ),
        LegalSection(
            heading = "Data Stored Locally",
            blocks = listOf(
                LegalBlock.Paragraph("Your installation of Storage Labels stores the following data in your database:"),
                LegalBlock.Bullet("User accounts (email, username, name, password hashes)"),
                LegalBlock.Bullet("Inventory data (locations, boxes, items)"),
                LegalBlock.Bullet("Uploaded images (encrypted at rest)"),
                LegalBlock.Bullet("User preferences and settings"),
            ),
        ),
        LegalSection(
            heading = "How Data is Used",
            blocks = listOf(
                LegalBlock.Paragraph("The software uses locally stored data to:"),
                LegalBlock.Bullet("Authenticate users (Local Authentication mode)"),
                LegalBlock.Bullet("Manage inventory (locations, boxes, items)"),
                LegalBlock.Bullet("Store and display images"),
                LegalBlock.Bullet("Track user preferences"),
                LegalBlock.Bullet("Generate QR codes for items"),
                LegalBlock.Paragraph("All data processing happens within your self-hosted instance. No data is transmitted to external services or the software developers."),
            ),
        ),
        LegalSection(
            heading = "Data Storage and Security",
            blocks = listOf(
                LegalBlock.Paragraph("The software implements the following security measures:"),
                LegalBlock.Bullet("Passwords are hashed using industry-standard algorithms"),
                LegalBlock.Bullet("Images are encrypted at rest using AES-256-GCM encryption"),
                LegalBlock.Bullet("Optional JWT token-based authentication"),
                LegalBlock.Bullet("Role-based access control (Admin, Auditor, User)"),
                LegalBlock.Paragraph("As the operator, you are responsible for:"),
                LegalBlock.Bullet("Securing your database and file storage"),
                LegalBlock.Bullet("Configuring HTTPS/TLS for network traffic"),
                LegalBlock.Bullet("Implementing network security (firewalls, VPNs, etc.)"),
                LegalBlock.Bullet("Regular security updates and patches"),
                LegalBlock.Bullet("Access control to your infrastructure"),
            ),
        ),
        LegalSection(
            heading = "Third-Party Services",
            blocks = listOf(
                LegalBlock.Paragraph("The software does not use any external third-party services by default. All data remains within your self-hosted environment."),
                LegalBlock.Paragraph("If you choose to integrate additional services (email providers, external authentication, cloud storage, etc.), you are responsible for understanding and complying with their privacy policies."),
            ),
        ),
        LegalSection(
            heading = "User Rights and Data Control",
            blocks = listOf(
                LegalBlock.Paragraph("As a self-hosted solution, you (as the operator) have complete control over all data:"),
                LegalBlock.Bullet("Direct database access for data export or deletion"),
                LegalBlock.Bullet("Ability to backup and restore all data"),
                LegalBlock.Bullet("Complete audit trail through database logs"),
                LegalBlock.Bullet("User management through admin interface"),
                LegalBlock.Paragraph("Users of your installation should contact you (the operator) for any data access, modification, or deletion requests."),
            ),
        ),
        LegalSection(
            heading = "Data Retention",
            blocks = listOf(
                LegalBlock.Paragraph("Data is retained in your installation until you (the operator) choose to delete it. The software provides:"),
                LegalBlock.Bullet("User deletion functionality (removes user and associated data)"),
                LegalBlock.Bullet("Manual database cleanup if needed"),
                LegalBlock.Bullet("Soft-delete options for some data types"),
                LegalBlock.Paragraph("You are responsible for implementing data retention policies that comply with applicable laws in your jurisdiction (GDPR, CCPA, etc.)."),
            ),
        ),
        LegalSection(
            heading = "Children's Privacy",
            blocks = listOf(
                LegalBlock.Paragraph("The software does not include age verification. As the operator, you are responsible for:"),
                LegalBlock.Bullet("Determining appropriate age restrictions for your installation"),
                LegalBlock.Bullet("Obtaining parental consent if required by law"),
                LegalBlock.Bullet("Complying with children's privacy laws (COPPA, etc.) in your jurisdiction"),
            ),
        ),
        LegalSection(
            heading = "Your Responsibilities as Operator",
            blocks = listOf(
                LegalBlock.Paragraph("By operating this software, you become the data controller and are responsible for:"),
                LegalBlock.Bullet("Creating and publishing your own privacy policy for your users"),
                LegalBlock.Bullet("Complying with applicable privacy laws (GDPR, CCPA, etc.)"),
                LegalBlock.Bullet("Implementing required user consent mechanisms"),
                LegalBlock.Bullet("Handling data subject requests (access, deletion, portability)"),
                LegalBlock.Bullet("Reporting data breaches as required by law"),
                LegalBlock.Bullet("Maintaining appropriate security measures"),
            ),
        ),
        LegalSection(
            heading = "Questions and Support",
            blocks = listOf(
                LegalBlock.Paragraph("For questions about the software's data handling:"),
                LegalBlock.Bullet("Review the source code (open-source)"),
                LegalBlock.Bullet("Check documentation in the repository"),
                LegalBlock.Bullet("Ask in community discussions"),
                LegalBlock.Paragraph("For privacy concerns about a specific installation, contact that installation's operator."),
            ),
        ),
        LegalSection(
            heading = "Changes to This Notice",
            blocks = listOf(
                LegalBlock.Paragraph("This privacy notice may be updated with new software versions. Check the documentation when updating the software for any changes to data handling practices."),
            ),
        ),
    ),
)

val TermsAndConditions = LegalDocument(
    title = "Software License Agreement for Storage Labels",
    sections = listOf(
        LegalSection(
            heading = "1. License Grant",
            blocks = listOf(
                LegalBlock.Paragraph("Storage Labels is open-source, self-hosted inventory management software. By installing and using this software, you agree to the terms of the applicable open-source license. This software is provided \"as-is\" for your personal or organizational use on infrastructure you control."),
            ),
        ),
        LegalSection(
            heading = "2. Self-Hosted Software",
            blocks = listOf(
                LegalBlock.Paragraph("Storage Labels is self-hosted software that you install and run on your own infrastructure. The software provides:"),
                LegalBlock.Bullet("Inventory management for physical storage items"),
                LegalBlock.Bullet("Location and box organization"),
                LegalBlock.Bullet("Image storage with encryption"),
                LegalBlock.Bullet("Search and tracking capabilities"),
                LegalBlock.Bullet("QR code generation"),
                LegalBlock.Paragraph("As the installer and operator of this software, you are responsible for its security, availability, and compliance with applicable laws."),
            ),
        ),
        LegalSection(
            heading = "3. Your Responsibilities",
            blocks = listOf(
                LegalBlock.Paragraph("As the installer and operator of this self-hosted software, you are responsible for:"),
                LegalBlock.Bullet("Compliance with all applicable laws and regulations in your jurisdiction"),
                LegalBlock.Bullet("Security of your installation and infrastructure"),
                LegalBlock.Bullet("Data backup and disaster recovery"),
                LegalBlock.Bullet("User access management and authentication"),
                LegalBlock.Bullet("Privacy and data protection of any users you grant access to"),
                LegalBlock.Bullet("Proper configuration and maintenance of the software"),
            ),
        ),
        LegalSection(
            heading = "4. Authentication",
            blocks = listOf(
                LegalBlock.Paragraph("Storage Labels supports two authentication modes:"),
                LegalBlock.Bullet("Local Authentication: Users authenticate with username/password managed by the system"),
                LegalBlock.Bullet("No Authentication: Open access mode suitable only for trusted networks"),
                LegalBlock.Paragraph("You are responsible for choosing an appropriate authentication mode for your deployment environment and managing user access accordingly."),
            ),
        ),
        LegalSection(
            heading = "5. Data Ownership",
            blocks = listOf(
                LegalBlock.Paragraph("All data stored in your self-hosted instance of Storage Labels belongs to you. This includes:"),
                LegalBlock.Bullet("User accounts and profiles"),
                LegalBlock.Bullet("Inventory data (boxes, items, locations)"),
                LegalBlock.Bullet("Uploaded images"),
                LegalBlock.Bullet("Configuration and preferences"),
                LegalBlock.Paragraph("As a self-hosted solution, no data is transmitted to or stored by the software developers. You maintain complete control over your data."),
            ),
        ),
        LegalSection(
            heading = "6. Content Guidelines",
            blocks = listOf(
                LegalBlock.Paragraph("When uploading images or creating content, you agree that:"),
                LegalBlock.Bullet("You own or have the right to use all content you upload"),
                LegalBlock.Bullet("Your content does not violate any laws or third-party rights"),
                LegalBlock.Bullet("Your content is not offensive, illegal, or inappropriate"),
                LegalBlock.Bullet("You will not upload excessively large files that abuse storage limits"),
            ),
        ),
        LegalSection(
            heading = "7. Software Updates",
            blocks = listOf(
                LegalBlock.Paragraph("Storage Labels is open-source software that may receive updates and improvements. However:"),
                LegalBlock.Bullet("Updates are provided on a best-effort basis"),
                LegalBlock.Bullet("You choose when and if to apply updates to your installation"),
                LegalBlock.Bullet("Backward compatibility is not guaranteed between versions"),
                LegalBlock.Bullet("You are responsible for testing updates before deploying to production"),
                LegalBlock.Paragraph("The availability and performance of your installation is your responsibility as the operator."),
            ),
        ),
        LegalSection(
            heading = "8. Data Backup and Recovery",
            blocks = listOf(
                LegalBlock.Paragraph("As the operator of this self-hosted software, you are solely responsible for:"),
                LegalBlock.Bullet("Implementing backup procedures for your database and uploaded files"),
                LegalBlock.Bullet("Testing backup restoration regularly"),
                LegalBlock.Bullet("Maintaining disaster recovery plans"),
                LegalBlock.Bullet("Securing backup data appropriately"),
                LegalBlock.Paragraph("The software developers provide no backup services and are not liable for any data loss."),
            ),
        ),
        LegalSection(
            heading = "9. Limitation of Liability and Warranty Disclaimer",
            blocks = listOf(
                LegalBlock.Paragraph("This software is provided \"as is\" without warranty of any kind, either express or implied, including but not limited to warranties of merchantability, fitness for a particular purpose, or non-infringement."),
                LegalBlock.Paragraph("The developers and contributors shall not be liable for any direct, indirect, incidental, special, consequential, or punitive damages, including without limitation, loss of profits, data, use, or other intangible losses, resulting from:"),
                LegalBlock.Bullet("Your installation, configuration, or use of the software"),
                LegalBlock.Bullet("Security vulnerabilities or unauthorized access"),
                LegalBlock.Bullet("Data loss, corruption, or breaches"),
                LegalBlock.Bullet("Software bugs, errors, or incompatibilities"),
                LegalBlock.Bullet("Any other use of this self-hosted software"),
            ),
        ),
        LegalSection(
            heading = "10. Indemnification",
            blocks = listOf(
                LegalBlock.Paragraph("You agree to indemnify and hold harmless the developers, contributors, and maintainers of Storage Labels from any claims, damages, losses, liabilities, and expenses arising from:"),
                LegalBlock.Bullet("Your installation and operation of the software"),
                LegalBlock.Bullet("Your users' access to and use of your installation"),
                LegalBlock.Bullet("Any data breaches or security incidents"),
                LegalBlock.Bullet("Your failure to comply with applicable laws and regulations"),
            ),
        ),
        LegalSection(
            heading = "11. Open Source License",
            blocks = listOf(
                LegalBlock.Paragraph("Storage Labels is licensed under an open-source license. You are free to:"),
                LegalBlock.Bullet("Use the software for personal or commercial purposes"),
                LegalBlock.Bullet("Modify the source code to suit your needs"),
                LegalBlock.Bullet("Distribute copies of the software"),
                LegalBlock.Bullet("Contribute improvements back to the project"),
                LegalBlock.Paragraph("Please refer to the LICENSE file in the source repository for the complete license terms."),
            ),
        ),
        LegalSection(
            heading = "12. Changes to Terms",
            blocks = listOf(
                LegalBlock.Paragraph("These terms may be updated from time to time. Changes will be reflected in software updates. It is your responsibility to review the terms when updating the software."),
            ),
        ),
        LegalSection(
            heading = "13. Governing Law",
            blocks = listOf(
                LegalBlock.Paragraph("These terms shall be governed by and construed in accordance with applicable laws, without regard to conflict of law provisions."),
            ),
        ),
        LegalSection(
            heading = "14. Support and Community",
            blocks = listOf(
                LegalBlock.Paragraph("As self-hosted open-source software, support is community-driven. For questions, issues, or contributions:"),
                LegalBlock.Bullet("Check the documentation in the repository"),
                LegalBlock.Bullet("Search existing issues on GitHub"),
                LegalBlock.Bullet("Join community discussions"),
                LegalBlock.Bullet("Submit bug reports or feature requests"),
                LegalBlock.Paragraph("No official support is guaranteed. Community assistance is provided on a best-effort basis."),
            ),
        ),
    ),
)
