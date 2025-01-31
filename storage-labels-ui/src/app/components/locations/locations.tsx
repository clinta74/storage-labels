import { Box } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useUser } from '../../providers/user-provider';

export const Locations: React.FC = () => {
    const { user, updateUser }= useUser();
    
    return (
        <Box>Showing Locations for {user.firstName}</Box>
    );
}