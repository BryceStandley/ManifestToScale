import { DateTime } from "luxon";


export class Utils {

	/**
	 * Returns the current date and time in the Australia/Perth timezone formatted as 'yyyy-MM-dd HH:mm:ss'.
	 * @returns {string} The current date and time in the specified format.
	 */
	static get CurrentDateTimeAWST(): string {
		return DateTime.now().setZone('Australia/Perth').toFormat('yyyy-MM-dd HH:mm:ss');
	}

	static get CurrentAWSTDateTime(): DateTime {
		return DateTime.now().setZone('Australia/Perth');
	}

	/**
	 * Returns the current date and time in the Australia/Perth timezone formatted as 'dd-MM-yyyy HH:mm:ss'.
	 * @returns {string} The current date and time in the specified format.
	 */
	static get CurrentDateTimeAWSTShort(): string {
		//return DateTime.local({ zone: "Australia/Perth" }).toFormat('dd-MM-yyyy HH:mm');
		return DateTime.now().setZone('Australia/Perth').toFormat('dd-MM-yyyy HH:mm');
	}

	/**
	 * Returns the current date and time in the Australia/Perth timezone in ISO format.
	 * @returns {string | null} The current date and time in ISO format, or null if an error occurs.
	 */
	static get CurrentDateTimeAWSTISO(): string | null {
		return DateTime.now().setZone('Australia/Perth').toISO();
	}

	/**
	 * Returns the current date in the Australia/Perth timezone formatted as 'dd-MM-yyyy'.
	 * @returns {string} The current date in the specified format.
	 */
	static get CurrentDateAus(): string {
		return DateTime.now().setZone('Australia/Perth').toFormat('dd-MM-yyyy');
	}

	/**
	 * Returns the current date and time in UTC formatted as 'yyyy-MM-dd HH:mm:ss'.
	 * @returns {string} The current date and time in UTC in the specified format.
	 */
	static get CurrentDateTimeUTC(): string {
		return DateTime.now().toUTC().toFormat('yyyy-MM-dd HH:mm:ss');
	}

	/**
	 * Returns the current date and time in UTC in ISO format.
	 * @returns {string | null} The current date and time in UTC in ISO format, or null if an error occurs.
	 */
	static get CurrentDateTimeUTCISO(): string | null {
		return DateTime.now().toUTC().toISO();
	}

	/**
	 * Converts a DateTime object to the Australia/Perth timezone.
	 * @param {DateTime} dateTime - The DateTime object to convert.
	 * @returns {DateTime} The converted DateTime object in the Australia/Perth timezone.
	 */
	static convertDateTimeToAWST(dateTime: DateTime): DateTime {
		return dateTime.setZone('Australia/Perth');
	}

	/**
	 * Converts a DateTime object to UTC.
	 * @param {DateTime} dateTime - The DateTime object to convert.
	 * @returns {DateTime} The converted DateTime object in UTC.
	 */
	static convertDateTimeToUTC(dateTime: DateTime): DateTime {
		return dateTime.toUTC();
	}

	/**
	 * Converts a date string in ISO format to the Australia/Perth timezone and formats it as 'dd-MM-yyyy'.
	 * @param {string} date - The date string in ISO format.
	 * @returns {string} The formatted date string in 'dd-MM-yyyy' format.
	 */
	static convertDateToAWSTandFormat(date: string): string {
		const dateTime = DateTime.fromISO(date, { zone: 'Australia/Perth' });
		return dateTime.toFormat('dd-MM-yyyy');
	}

	/**
	 * Converts a date string in ISO format to the Australia/Perth timezone and formats it as 'dd-MM-yyyy HH:mm:ss'.
	 * @param {string} date - The date string in ISO format.
	 * @returns {string} The formatted date string in 'dd-MM-yyyy HH:mm:ss' format.
	 */
	static getEmailDeliveryDateTime(date: string): string {
		const dateTime = DateTime.fromISO(date, { zone: 'Australia/Perth' });
		return dateTime.set({ hour: 5, minute: 0, second: 0 }).toFormat('dd-MM-yyyy HH:mm:ss');
	}

	static getEmailDeliveryDT(date: string): DateTime {
		const dateTime = DateTime.fromISO(date, { zone: 'Australia/Perth' });
		return dateTime.set({ hour: 5, minute: 0, second: 0 });
	}

	/**
	 * Converts a date string in ISO format to UTC and formats it as 'yyyy-MM-dd HH:mm:ss'.
	 * @param {string} date - The date string in ISO format.
	 * @returns {string} The formatted date string in 'yyyy-MM-dd HH:mm:ss' format.
	 */
	static getEmailDeliveryDateTimeUTC(date: string): string {
		const dateTime = DateTime.fromISO(date, { zone: 'Australia/Perth' });
		return dateTime.set({ hour: 5, minute: 0, second: 0 }).toUTC().toFormat('yyyy-MM-dd HH:mm:ss');
	}

	 /**
	 * Converts a date string in ISO format to RFC 2822 format.
	 * @param {string} date - The date string in ISO format.
	 * @returns {string | null} The date string in RFC 2822 format, or null if an error occurs.
	 */
	static getEmailDeliveryDateTimeRF2822(date: string): string | null {
		const dateTime = DateTime.fromISO(date, { zone: 'Australia/Perth' });
		return dateTime.set({ hour: 5, minute: 0, second: 0 }).toUTC().toRFC2822();
	}

	/**
	 * Converts a date string in ISO format to UTC and returns it in ISO format.
	 * @param {string} date - The date string in ISO format.
	 * @returns {string | null} The date string in UTC ISO format, or null if an error occurs.
	 */
	static getEmailDeliveryDateTimeISO(date: string): string | null {
		const dateTime = DateTime.fromISO(date, { zone: 'Australia/Perth' });
		return dateTime.set({ hour: 5, minute: 0, second: 0 }).toUTC().toISO();
	}

	/**
	 * Converts a date string in ISO format to a simple scale date string formatted as 'ddMMyyyy'.
	 * @param {string} date - The date string in ISO format.
	 * @returns {string} The formatted date string in 'ddMMyyyy' format.
	 */
	static GetSimpleScaleDateString(date: string): string {
		const dateTime = DateTime.fromISO(date, { zone: 'Australia/Perth' });
		return dateTime.toFormat('ddMMyyyy');
	}
}
