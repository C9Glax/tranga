export const splitCamelCase = (str: string): string => str.replace(/([a-z])([A-Z])/g, '$1 $2');
