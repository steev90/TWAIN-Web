﻿using System;
using System.Collections.Generic;
using System.Threading;
using log4net;
using TwainWeb.Standalone.App.Models;
using TwainWeb.Standalone.Scanner;

namespace TwainWeb.Standalone.App.Queries
{
	public class GetScannerParametersQuery
	{
		private readonly IScannerManager _scannerManager;
		private readonly CashSettings _cashSettings;
		private int? _sourceIndex;
		private readonly ILog _logger;
		private const int WaitTime = 15000;
		public GetScannerParametersQuery(IScannerManager scannerManager, CashSettings cashSettings, int? sourceIndex)
		{
			if (scannerManager == null) throw new Exception("Невозможно получить параметры сканирования, т.к. менеджер источников данных не был инициализирован");

			_scannerManager = scannerManager;
			_cashSettings = cashSettings;
			_sourceIndex = sourceIndex;

			_logger = LogManager.GetLogger(typeof(HttpServer));
		}
		public ScannerParametersQueryResult Execute(object markerAsync)
		{
			ScannerSettings searchSetting = null;
			List<ISource> sources = null;

			if (Monitor.TryEnter(markerAsync))
			{
				try
				{
					var sourcesCount = _scannerManager.SourceCount;

					if (sourcesCount > 0)
					{
						//если выбранный источник существует, выбираем его; если нет - выбираем первый
						int sourceIndex;
						if (!_sourceIndex.HasValue || (_sourceIndex.Value > sourcesCount - 1))
							sourceIndex = 0;
						else
							sourceIndex = _sourceIndex.Value;


						if (_cashSettings.NeedUpdateNow(DateTime.UtcNow))
						{
							_cashSettings.Update(_scannerManager);
						}

						try
						{
							searchSetting = GetScannerSettings(sourceIndex);
						}
						catch (Exception)
						{
							_logger.Error("Can't obtain scanner settings");
						}

						sources = _scannerManager.GetSources();
					}
				}
				catch (Exception e)
				{
					return new ScannerParametersQueryResult(string.Format("Ошибка при получении информации об источниках: {0}", e));
				}
				finally
				{
					Monitor.Exit(markerAsync);
				}
			}
			else
			{
				return new ScannerParametersQueryResult(string.Format("Не удалось получить информацию об источниках: сканер занят"));
			}

			return new ScannerParametersQueryResult(sources, searchSetting, _sourceIndex);
		}

		private ScannerSettings GetScannerSettings(int sourceIndex)
		{
			var searchSetting = _cashSettings.Search(_scannerManager, sourceIndex);
			if (searchSetting != null) return searchSetting;

			var needOfChangeSource = _sourceIndex.HasValue && _sourceIndex != _scannerManager.CurrentSourceIndex;

			if (needOfChangeSource)
				new AsyncWorker<int>().RunWorkAsync(sourceIndex, "ChangeSource", _scannerManager.ChangeSource, WaitTime);

			if (_scannerManager.CurrentSource == null)
				throw new Exception("Не удалось выбрать источник");

			if (sourceIndex == _scannerManager.CurrentSource.Index)
				searchSetting = _cashSettings.PushCurrentSource(_scannerManager);

			return searchSetting;
		}
	}
}			