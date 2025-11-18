import React from "react"
import { Box, Container, Paper, Typography, Link } from '@mui/material';

export const PrivacyPolicy: React.FC = () => {
    return (
        <Container maxWidth="md" sx={{ py: 4 }}>
            <Paper elevation={3} sx={{ p: 4 }}>
                <Typography variant="h3" component="h1" gutterBottom>
                    Privacy Notice for Storage Labels
                </Typography>
                
                <Typography variant="body2" color="text.secondary" gutterBottom>
                    Last updated: {new Date().toLocaleDateString()}
                </Typography>

                <Box sx={{ mt: 4 }}>
                    <Typography variant="h5" gutterBottom>
                        Important: Self-Hosted Software
                    </Typography>
                    <Typography paragraph>
                        Storage Labels is self-hosted software that you install and operate on your own infrastructure. 
                        <strong> The software developers do not collect, store, or have access to any of your data.</strong>
                    </Typography>
                    <Typography paragraph>
                        This notice describes how the software handles data locally in your installation. As the operator, 
                        you are the data controller and are responsible for compliance with applicable privacy laws.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Data Stored Locally
                    </Typography>
                    <Typography paragraph>
                        Your installation of Storage Labels stores the following data in your database:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>User accounts (email, username, name, password hashes)</li>
                        <li>Inventory data (locations, boxes, items)</li>
                        <li>Uploaded images (encrypted at rest)</li>
                        <li>User preferences and settings</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        How Data is Used
                    </Typography>
                    <Typography paragraph>
                        The software uses locally stored data to:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Authenticate users (Local Authentication mode)</li>
                        <li>Manage inventory (locations, boxes, items)</li>
                        <li>Store and display images</li>
                        <li>Track user preferences</li>
                        <li>Generate QR codes for items</li>
                    </Box>
                    <Typography paragraph>
                        All data processing happens within your self-hosted instance. No data is transmitted to 
                        external services or the software developers.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Data Storage and Security
                    </Typography>
                    <Typography paragraph>
                        The software implements the following security measures:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Passwords are hashed using industry-standard algorithms</li>
                        <li>Images are encrypted at rest using AES-256-GCM encryption</li>
                        <li>Optional JWT token-based authentication</li>
                        <li>Role-based access control (Admin, Auditor, User)</li>
                    </Box>
                    <Typography paragraph>
                        <strong>As the operator, you are responsible for:</strong>
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Securing your database and file storage</li>
                        <li>Configuring HTTPS/TLS for network traffic</li>
                        <li>Implementing network security (firewalls, VPNs, etc.)</li>
                        <li>Regular security updates and patches</li>
                        <li>Access control to your infrastructure</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Third-Party Services
                    </Typography>
                    <Typography paragraph>
                        The software does not use any external third-party services by default. All data remains 
                        within your self-hosted environment.
                    </Typography>
                    <Typography paragraph>
                        If you choose to integrate additional services (email providers, external authentication, 
                        cloud storage, etc.), you are responsible for understanding and complying with their privacy policies.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        User Rights and Data Control
                    </Typography>
                    <Typography paragraph>
                        As a self-hosted solution, you (as the operator) have complete control over all data:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Direct database access for data export or deletion</li>
                        <li>Ability to backup and restore all data</li>
                        <li>Complete audit trail through database logs</li>
                        <li>User management through admin interface</li>
                    </Box>
                    <Typography paragraph>
                        Users of your installation should contact you (the operator) for any data access, 
                        modification, or deletion requests.
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Data Retention
                    </Typography>
                    <Typography paragraph>
                        Data is retained in your installation until you (the operator) choose to delete it. 
                        The software provides:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>User deletion functionality (removes user and associated data)</li>
                        <li>Manual database cleanup if needed</li>
                        <li>Soft-delete options for some data types</li>
                    </Box>
                    <Typography paragraph>
                        You are responsible for implementing data retention policies that comply with applicable laws 
                        in your jurisdiction (GDPR, CCPA, etc.).
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Children's Privacy
                    </Typography>
                    <Typography paragraph>
                        The software does not include age verification. As the operator, you are responsible for:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Determining appropriate age restrictions for your installation</li>
                        <li>Obtaining parental consent if required by law</li>
                        <li>Complying with children's privacy laws (COPPA, etc.) in your jurisdiction</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Your Responsibilities as Operator
                    </Typography>
                    <Typography paragraph>
                        By operating this software, you become the data controller and are responsible for:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Creating and publishing your own privacy policy for your users</li>
                        <li>Complying with applicable privacy laws (GDPR, CCPA, etc.)</li>
                        <li>Implementing required user consent mechanisms</li>
                        <li>Handling data subject requests (access, deletion, portability)</li>
                        <li>Reporting data breaches as required by law</li>
                        <li>Maintaining appropriate security measures</li>
                    </Box>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Questions and Support
                    </Typography>
                    <Typography paragraph>
                        For questions about the software's data handling:
                    </Typography>
                    <Box component="ul" sx={{ pl: 3 }}>
                        <li>Review the source code (open-source)</li>
                        <li>Check documentation in the repository</li>
                        <li>Ask in community discussions</li>
                    </Box>
                    <Typography paragraph>
                        <strong>For privacy concerns about a specific installation, contact that installation's operator.</strong>
                    </Typography>

                    <Typography variant="h5" gutterBottom sx={{ mt: 3 }}>
                        Changes to This Notice
                    </Typography>
                    <Typography paragraph>
                        This privacy notice may be updated with new software versions. Check the documentation when 
                        updating the software for any changes to data handling practices.
                    </Typography>
                </Box>

                <Box sx={{ mt: 4, textAlign: 'center' }}>
                    <Link href="/" underline="hover">
                        Return to Storage Labels
                    </Link>
                </Box>
            </Paper>
        </Container>
    );
}